// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Utils;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

/// <summary>
/// Represents a .msapp file.
/// </summary>
public class MsappArchive : IMsappArchive, IDisposable
{
    #region Fields

    private readonly Lazy<IDictionary<string, ZipArchiveEntry>> _canonicalEntries;
    private bool _isDisposed;
    private readonly ILogger<MsappArchive>? _logger;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    #endregion

    #region Constructors

    public MsappArchive(string path, ILogger<MsappArchive>? logger = null)
        : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, leaveOpen: false, logger)
    {
    }

    public MsappArchive(Stream stream, ILogger<MsappArchive>? logger = null)
        : this(stream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, ILogger<MsappArchive>? logger = null)
        : this(stream, mode, leaveOpen: false, entryNameEncoding: null, logger)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="mode"></param>
    /// <param name="leaveOpen">
    ///     true to leave the stream open after the System.IO.Compression.ZipArchive object is disposed; otherwise, false
    /// </param>
    /// <param name="logger"></param>
    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, ILogger<MsappArchive>? logger = null)
        : this(stream, mode, leaveOpen, null, logger)
    {
    }

    public MsappArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding, ILogger<MsappArchive>? logger = null)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _logger = logger;
        ZipArchive = new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);
        _canonicalEntries = new Lazy<IDictionary<string, ZipArchiveEntry>>
        (() =>
        {
            var canonicalEntries = new Dictionary<string, ZipArchiveEntry>();

            // If we're creating a new archive, there are no entries to canonicalize.
            if (mode == ZipArchiveMode.Create)
                return canonicalEntries;

            foreach (var entry in ZipArchive.Entries)
            {
                if (!canonicalEntries.TryAdd(FileUtils.NormalizePath(entry.FullName), entry))
                    _logger?.LogInformation($"Duplicate entry found in archive: {entry.FullName}");
            }

            return canonicalEntries;
        });
    }

    #endregion

    #region Properties

    /// <summary>
    /// Canonical entries in the archive.  Keys are normalized paths (lowercase, forward slashes, no trailing slash).
    /// </summary>
    public IReadOnlyDictionary<string, ZipArchiveEntry> CanonicalEntries => _canonicalEntries.Value.AsReadOnly();

    /// <inheritdoc/>
    public ZipArchive ZipArchive { get; private set; }

    /// <summary>
    /// Total sum of decompressed sizes of all entries in the archive.
    /// </summary>
    public long DecompressedSize => ZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.Length);

    /// <summary>
    /// Total sum of compressed sizes of all entries in the archive.
    /// </summary>
    public long CompressedSize => ZipArchive.Entries.Sum(zipArchiveEntry => zipArchiveEntry.CompressedLength);

    #endregion

    #region Methods

    /// <inheritdoc/>
    public IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string? extension = null)
    {
        _ = directoryName ?? throw new ArgumentNullException(nameof(directoryName));

        directoryName = FileUtils.NormalizePath(directoryName);

        foreach (var entry in CanonicalEntries)
        {
            if (!entry.Key.StartsWith(directoryName + '/'))
                continue;

            if (extension != null && !entry.Key.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return entry.Value;
        }
    }

    /// <inheritdoc/>
    public ZipArchiveEntry? GetEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            return null;

        entryName = FileUtils.NormalizePath(entryName);
        if (CanonicalEntries.TryGetValue(entryName, out var entry))
            return entry;

        return null;
    }

    /// <inheritdoc/>
    public ZipArchiveEntry CreateEntry(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or whitespace.", nameof(entryName));

        var canonicalEntryName = FileUtils.NormalizePath(entryName);
        if (_canonicalEntries.Value.ContainsKey(canonicalEntryName))
            throw new InvalidOperationException($"Entry {entryName} already exists in the archive.");

        var entry = ZipArchive.CreateEntry(entryName);
        _canonicalEntries.Value.Add(canonicalEntryName, entry);

        return entry;
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (!_leaveOpen)
                {
                    ZipArchive.Dispose();
                    _stream.Dispose();
                }
            }

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

    public static class Directories
    {
        public const string Src = "Src";
        public const string Controls = "Controls";
        public const string Components = "Components";
        public const string AppTests = "AppTests";
        public const string References = "References";
        public const string Resources = "Resources";
    }
}

