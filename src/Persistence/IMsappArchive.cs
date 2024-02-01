// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

/// <summary>
/// base interface for MsappArchive
/// </summary>
public interface IMsappArchive : IDisposable
{
    /// <summary>
    /// The app that is represented by the archive.
    /// </summary>
    App? App { get; set; }

    /// <summary>
    /// Creates a new entry in the archive with the given name.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    ZipArchiveEntry CreateEntry(string entryName);

    /// <summary>
    /// Returns the entry in the archive with the given name.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns>the entry or null when not found.</returns>
    ZipArchiveEntry? GetEntry(string entryName);

    /// <summary>
    /// Returns all entries in the archive that are in the given directory.
    /// </summary>
    /// <param name="directoryName"></param>
    /// <param name="extension"></param>
    /// <returns></returns>
    IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string? extension = null);

    /// <summary>
    /// Provides access to the underlying zip archive.
    /// </summary>
    ZipArchive ZipArchive { get; }

    /// <summary>
    /// Saves the archive to the given stream or file.
    /// </summary>
    void Save();
}
