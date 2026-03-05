// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Represents a package of compressed files in zip format using normalized semantics for Power Apps tooling.
/// </summary>
public interface IPaArchive : IDisposable
{
    ZipArchiveMode Mode { get; }

    /// <summary>
    /// Total sum of decompressed sizes of all entries in the archive.
    /// </summary>
    long DecompressedSize { get; }

    /// <summary>
    /// Total sum of compressed sizes of all entries in the archive.
    /// </summary>
    long CompressedSize { get; }

    /// <summary>
    /// Gets a read-only collection containing all file entries in the archive.
    /// </summary>
    ReadOnlyCollection<PaArchiveEntry> Entries { get; }

    /// <summary>
    /// Determine whether an entry with the given path exists in the archive.
    /// </summary>
    bool ContainsEntry(string entryPath);

    /// <summary>
    /// Creates a new entry in the archive with the given name.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    PaArchiveEntry CreateEntry(string entryName);

    /// <summary>
    /// Returns the entry in the archive with the given name or null when not found.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns>the entry or null when not found.</returns>
    PaArchiveEntry? GetEntryOrDefault(string entryName);

    /// <summary>
    /// Returns the entry in the archive with the given name or null when not found.
    /// </summary>
    /// <param name="entryName"></param>
    /// <param name="zipArchiveEntry"></param>
    /// <returns></returns>
    bool TryGetEntry(string entryName, [NotNullWhen(true)] out PaArchiveEntry? zipArchiveEntry);

    /// <summary>
    /// Returns the entry in the archive with the given name or throws if it does not exist.
    /// </summary>
    PaArchiveEntry GetRequiredEntry(string entryName);

    /// <summary>
    /// Returns entries in the archive in the specified directory.
    /// </summary>
    IEnumerable<PaArchiveEntry> GetEntriesInDirectory(string directoryName, string? extension = null, bool recursive = false);
}
