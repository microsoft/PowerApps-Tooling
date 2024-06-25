// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// base interface for MsappArchive
/// </summary>
public interface IMsappArchive : IDisposable
{
    /// <summary>
    /// The app that is represented by the archive.
    /// </summary>
    App? App { get; set; }

    Version Version { get; }

    Version DocVersion { get; }

    AppProperties? Properties { get; set; }

    DataSources? DataSources { get; set; }

    Resources? Resources { get; set; }

    T Deserialize<T>(string entryName, bool ensureRoundTrip = true) where T : Control;

    /// <summary>
    /// Saves control in the archive. Control can be App, Screen, or individual control.
    /// </summary>
    void Save(Control control, string? directory = null);

    /// <summary>
    /// Saves the archive to the given stream or file.
    /// </summary>
    void Save();

    /// <summary>
    /// Total sum of decompressed sizes of all entries in the archive.
    /// </summary>
    long DecompressedSize { get; }

    /// <summary>
    /// Total sum of compressed sizes of all entries in the archive.
    /// </summary>
    long CompressedSize { get; }

    /// <summary>
    /// Adds an image to the archive and registers it as a resource.
    /// </summary>
    /// <returns>the name of the resource</returns>
    string AddImage(string fileName, Stream imageStream);

    /// <summary>
    /// Determine whether an entry with the given path exists in the archive.
    /// </summary>
    bool DoesEntryExist(string entryPath);

    /// <summary>
    /// Creates a new entry in the archive with the given name.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    ZipArchiveEntry CreateEntry(string entryName);

    /// <summary>
    /// Attempts to generate a unique entry path in the specified directory, based on a starter file name and extension.
    /// </summary>
    /// <param name="directory">The directory path for the entry; or null. Note: Directory path is expected to already be safe, as it is expected to not contain customer data.</param>
    /// <param name="fileNameNoExtension">The name of the file, without an extension. This file name should already have been made 'safe' by the caller. If needed, use <see cref="MsappArchive.TryMakeSafeForEntryPathSegment"/> to make it safe.</param>
    /// <param name="extension">The file extension to add for the file path or null for no extension. Note: This should already contain only safe chars, as it is expected to not contain customer data.</param>
    /// <param name="uniqueSuffixSeparator">The string added just before the unique file number and extension. Default is empty string.</param>
    /// <returns>The entry path which is unique in the specified <paramref name="directory"/>.</returns>
    /// <exception cref="ArgumentException">directory is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">A unique filename entry could not be generated.</exception>
    string GenerateUniqueEntryPath(
        string? directory,
        string fileNameNoExtension,
        string? extension,
        string uniqueSuffixSeparator = "");

    /// <summary>
    /// Returns the entry in the archive with the given name or null when not found.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns>the entry or null when not found.</returns>
    ZipArchiveEntry? GetEntry(string entryName);

    /// <summary>
    /// Returns the entry in the archive with the given name or null when not found.
    /// </summary>
    /// <param name="entryName"></param>
    /// <param name="zipArchiveEntry"></param>
    /// <returns></returns>
    bool TryGetEntry(string entryName, [MaybeNullWhen(false)] out ZipArchiveEntry zipArchiveEntry);

    /// <summary>
    /// Returns the entry in the archive with the given name or throws if it does not exist.
    /// </summary>
    ZipArchiveEntry GetRequiredEntry(string entryName);

    /// <summary>
    /// Returns all entries in the archive that are in the given directory.
    /// </summary>
    IEnumerable<ZipArchiveEntry> GetDirectoryEntries(string directoryName, string? extension = null, bool recursive = true);

    /// <summary>
    /// Dictionary of all entries in the archive.
    /// The keys are normalized paths for the entry computed using <see cref="MsappArchive.CanonicalizePath"/>.
    /// </summary>
    IReadOnlyDictionary<string, ZipArchiveEntry> CanonicalEntries { get; }

    /// <summary>
    /// Provides access to the underlying zip archive.
    /// Attention: This property might be removed in the future.
    /// </summary>
    ZipArchive ZipArchive { get; }
}
