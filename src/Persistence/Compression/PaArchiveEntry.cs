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
    /// The compressed size of the entry.
    /// If the archive that the entry belongs to is in Create mode, attempts to get this property will always throw an exception.
    /// If the archive that the entry belongs to is in update mode, this property will only be valid if the entry has not been opened.
    /// </summary>
    public long CompressedLength => ZipEntry.CompressedLength;

    /// <summary>
    /// The uncompressed size of the entry.
    /// This property is not valid in Create mode, and it is only valid in Update mode if the entry has not been opened.
    /// </summary>
    public long Length => ZipEntry.Length;

    /// <summary>
    /// Opens the entry.
    /// See additional docs for <see cref="ZipArchiveEntry.Open"/>.
    /// </summary>
    /// <returns>A Stream that represents the contents of the entry.</returns>
    public Stream Open() => ZipEntry.Open();

    /// <summary>
    /// Deletes the entry from the <see cref="PaArchive"/>.
    /// </summary>
    public void Delete()
    {
        ZipEntry.Delete();
        PaArchive.RemoveEntry(this);
    }
}
