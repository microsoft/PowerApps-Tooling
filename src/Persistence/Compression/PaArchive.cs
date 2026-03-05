// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Represents a package of compressed files in zip format using normalized semantics for Power Apps tooling.
/// </summary>
/// <remarks>
/// Warning: This class is NOT thread-safe, same as the underlying <see cref="ZipArchive"/> APIs.
/// </remarks>
public partial class PaArchive : IPaArchive, IDisposable
{
    private readonly ILogger<PaArchive>? _logger;
    private readonly ZipArchive _innerZipArchive;
    private readonly List<PaArchiveEntry> _entries;
    private readonly ReadOnlyCollection<PaArchiveEntry> _entriesCollection;
    private readonly Dictionary<PaArchivePath, PaArchiveEntry> _entriesDictionary;
    private bool _isDisposed;
    private bool _isEntriesInitialized;

    /// <summary>
    /// Initializes a new instance of <see cref="PaArchive"/> on the given stream in the specified mode, specifying whether to leave the stream open.
    /// </summary>
    /// <param name="leaveOpen">true to leave the stream open after the <see cref="PaArchive"/> object is disposed; otherwise, false</param>
    /// <param name="entryNameEncoding">See docs for same parameter in <see cref="ZipArchive.ZipArchive(Stream, ZipArchiveMode, bool, Encoding?)"/>.</param>
    public PaArchive(
        Stream stream,
        ZipArchiveMode mode,
        bool leaveOpen = false,
        Encoding? entryNameEncoding = null,
        ILogger<PaArchive>? logger = null)
    {
        _innerZipArchive = new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);
        _logger = logger;
        _entries = [];
        _entriesCollection = new(_entries);
        // Note: We don't need to use a custom comparer for the keys, as  PaArchivePath defines IEquitable<PaArchivePath>
        _entriesDictionary = [];

        // Perf: When creating a new archive, there aren't any entries existing already
        _isEntriesInitialized = mode == ZipArchiveMode.Create;
    }

    internal ZipArchive InnerZipArchive => _innerZipArchive;

    public ZipArchiveMode Mode => _innerZipArchive.Mode;

    protected bool IsDisposed => _isDisposed;

    /// <summary>
    /// Total sum of decompressed sizes of all entries in the archive.
    /// </summary>
    public long DecompressedSize => InnerZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.Length);

    /// <summary>
    /// Total sum of compressed sizes of all entries in the archive.
    /// </summary>
    public long CompressedSize => InnerZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.CompressedLength);

    /// <summary>
    /// Gets a read-only collection containing all file entries in the archive.
    /// </summary>
    public ReadOnlyCollection<PaArchiveEntry> Entries
    {
        get
        {
            ThrowIfDisposed();
            EnsureEntriesInitialized();
            return _entriesCollection;
        }
    }

    // Used to simplify entities are initialized before we access the dictionary
    private Dictionary<PaArchivePath, PaArchiveEntry> EntriesDictionary
    {
        get
        {
            ThrowIfDisposed();
            EnsureEntriesInitialized();
            return _entriesDictionary;
        }
    }

    private void EnsureEntriesInitialized()
    {
        if (!_isEntriesInitialized)
        {
            foreach (var zipEntry in InnerZipArchive.Entries)
            {
                if (!PaArchivePath.TryParse(zipEntry.FullName, out var normalizedPath, out var invalidReason))
                {
                    // Skip entries whose FullName is an invalid or malicious path (e.g. path traversal using '..', invalid chars).
                    // This must not block loading the rest of the archive.
                    _logger?.LogInvalidPathEntryIgnored(zipEntry.FullName, invalidReason.Value);
                }
                // To prevent blocking of opening an app, in DocSvr we choose to ignore root and directory entries.
                else if (normalizedPath.IsRoot)
                {
                    _logger?.LogNormalizedRootEntryIgnored(zipEntry.FullName);
                }
                else if (normalizedPath.IsDirectory)
                {
                    if (zipEntry.Length > 0)
                        _logger?.LogDirectoryEntryWithData(zipEntry.FullName, normalizedPath, zipEntry.Length);
                    _logger?.LogDirectoryEntryIgnored(zipEntry.FullName, normalizedPath);
                }
                else
                {
                    var paEntry = new PaArchiveEntry(this, zipEntry, normalizedPath);
                    if (!_entriesDictionary.TryAdd(paEntry.NormalizedPath, paEntry))
                    {
                        // To prevent blocking of opening an app, in DocSvr we choose to ignore duplicate entries.
                        // To make it deterministic, this logic keeps the first entry and ignores subsequent ones.
                        _logger?.LogDuplicateEntryIgnored(zipEntry.FullName, normalizedPath);
                    }
                    else
                    {
                        _entries.Add(paEntry);
                    }
                }
            }

            _isEntriesInitialized = true;
        }
    }

    public PaArchiveEntry CreateEntry(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return CreateEntry(PaArchivePath.ParseArgument(fullName));
    }

    public PaArchiveEntry CreateEntry(PaArchivePath entryPath)
    {
        if (entryPath.IsRoot || entryPath.IsDirectory)
            throw new ArgumentException($"Directory or root paths are not allowed as entries into a {GetType().Name}.", nameof(entryPath));

        if (ContainsEntry(entryPath))
            throw new InvalidOperationException($"An entry with path '{entryPath}' already exists in the archive.");

        var zipEntry = InnerZipArchive.CreateEntry(entryPath);
        var paEntry = new PaArchiveEntry(this, zipEntry, entryPath);
        _entriesDictionary.Add(paEntry.NormalizedPath, paEntry);
        _entries.Add(paEntry);

        return paEntry;
    }

    public bool ContainsEntry(string fullName)
    {
        return ContainsEntry(PaArchivePath.ParseArgument(fullName));
    }

    public bool ContainsEntry(PaArchivePath entryPath)
    {
        return EntriesDictionary.ContainsKey(entryPath);
    }

    public bool TryGetEntry(string fullName, [NotNullWhen(true)] out PaArchiveEntry? paEntry)
    {
        ArgumentNullException.ThrowIfNull(fullName);
        return TryGetEntry(PaArchivePath.ParseArgument(fullName), out paEntry);
    }

    public bool TryGetEntry(PaArchivePath entryPath, [NotNullWhen(true)] out PaArchiveEntry? paEntry)
    {
        ArgumentNullException.ThrowIfNull(entryPath);

        if (entryPath.IsRoot || entryPath.IsDirectory)
        {
            // we don't store root or directory entries
            paEntry = null;
            return false;
        }

        return EntriesDictionary.TryGetValue(entryPath, out paEntry);
    }

    public PaArchiveEntry? GetEntryOrDefault(string fullName)
    {
        return TryGetEntry(fullName, out var entry) ? entry : null;
    }

    public PaArchiveEntry GetRequiredEntry(string fullName)
    {
        // TODO: throw a new exception type, like PaArchiveException with a Reason enum
        return TryGetEntry(fullName, out var entry)
            ? entry
            : throw new PersistenceLibraryException(PersistenceErrorCode.PaArchiveMissingRequiredEntry, $"Entry with name '{fullName}' not found in msapp archive.");
    }

    /// <summary>
    /// Returns all entries in the archive that are in the given directory.
    /// </summary>
    public IEnumerable<PaArchiveEntry> GetEntriesInDirectory(string directoryName, string? extension = null, bool recursive = false)
    {
        var directoryPath = PaArchivePath.AsDirectoryOrRoot(directoryName);
        return GetEntriesInDirectory(directoryPath, extension, recursive);
    }

    public IEnumerable<PaArchiveEntry> GetEntriesInDirectory(PaArchivePath directoryPath, string? extension = null, bool recursive = false)
    {
        if (!directoryPath.IsDirectory && !directoryPath.IsRoot)
            throw new ArgumentException($"{nameof(directoryPath)} should be a directory or root path", nameof(directoryPath));

        foreach (var entry in Entries)
        {
            if (!directoryPath.ContainsPath(entry.NormalizedPath, nonRecursive: !recursive))
                continue;

            if (!string.IsNullOrEmpty(extension) && !entry.NormalizedPath.MatchesFileExtension(extension))
                continue;

            yield return entry;
        }
    }

    public void AddEntryFromJson<T>(string fullName, T value, JsonSerializerOptions serializerOptions)
    {
        var entry = CreateEntry(fullName);
        using var stream = entry.Open();
        JsonSerializer.Serialize(stream, value, serializerOptions);
    }

    public void AddEntryFrom(string fullName, PaArchiveEntry sourceEntry) => AddEntryFrom(PaArchivePath.ParseArgument(fullName), sourceEntry);

    public void AddEntryFrom(PaArchivePath entryPath, PaArchiveEntry sourceEntry)
    {
        if (sourceEntry.PaArchive.InnerZipArchive == InnerZipArchive)
        {
            throw new ArgumentException($"The {nameof(sourceEntry)} can not be from the same archive instance.", nameof(sourceEntry));
        }

        var newEntry = CreateEntry(entryPath);
        using var srcStream = sourceEntry.Open();
        using var destStream = newEntry.Open();
        srcStream.CopyTo(destStream);
    }

    #region IDisposable

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            // ZipArchive.Dispose() finishes writing the zip file with it's current contents when opened in Create or Update mode.
            // It also disposes the underlying stream unless leaveOpen was set to true.
            _innerZipArchive.Dispose();
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
