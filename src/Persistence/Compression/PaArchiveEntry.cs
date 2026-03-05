// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

public class PaArchiveEntry
{
    internal PaArchiveEntry(PaArchive paArchive, ZipArchiveEntry zipEntry, PaArchivePath normalizedPath)
    {
        Debug.Assert(zipEntry.Archive == paArchive.InnerZipArchive, "The underlying zip archives do not match.");
        Debug.Assert(
            PaArchivePath.TryParse(zipEntry.FullName, out var reparsedPath, out _) && normalizedPath.Equals(reparsedPath),
            $"normalizedPath '{normalizedPath}' does not match the path parsed from zipEntry.FullName '{zipEntry.FullName}'.");
        Debug.Assert(!normalizedPath.IsRoot, "PaArchiveEntry should never be created with a root path.");
        Debug.Assert(!normalizedPath.IsDirectory, "PaArchiveEntry should never be created with a directory path.");

        PaArchive = paArchive;
        ZipEntry = zipEntry;
        NormalizedPath = normalizedPath;
    }

    public PaArchive PaArchive { get; }

    /// <summary>
    /// The zip archive entry that this instance wraps.
    /// </summary>
    public ZipArchiveEntry ZipEntry { get; }

    public string OriginalFullName => ZipEntry.FullName;

    /// <summary>
    /// The normalized path for this entry.
    /// Used as a cross-platform safe key for the entry.
    /// </summary>
    public PaArchivePath NormalizedPath { get; }

    /// <summary>
    /// The normalized full name of the entry.
    /// </summary>
    public string FullName => NormalizedPath.FullName;

    public string Name => NormalizedPath.Name;

    /// <summary>
    /// Opens the entry.
    /// See additional docs for <see cref="ZipArchiveEntry.Open"/>.
    /// </summary>
    /// <returns>A Stream that represents the contents of the entry.</returns>
    public Stream Open() => ZipEntry.Open();
}
