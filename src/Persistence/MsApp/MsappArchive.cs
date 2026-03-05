// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Represents a .msapp file.
/// </summary>
/// <param name="leaveOpen">true to leave the stream open after the System.IO.Compression.ZipArchive object is disposed; otherwise, false</param>
public partial class MsappArchive(
    Stream stream,
    ZipArchiveMode mode,
    bool leaveOpen = false,
    ILogger<MsappArchive>? logger = null)
    : PaArchive(stream, mode, leaveOpen, entryNameEncoding: null, logger), IMsappArchive
{
    private HeaderJson? _header;

    /// <inheritdoc/>
    public ZipArchive ZipArchive => InnerZipArchive;

    internal HeaderJson Header => _header ??= LoadHeader();

    // When the header is missing the structure version, then it's semantically 1.0. i.e. legacy msapp.
    public Version MSAppStructureVersion => Header.MSAppStructureVersion ?? HeaderJson.MSAppV1_0Version;

    public Version DocVersion => Header.DocVersion;

    /// <inheritdoc/>
    public string GenerateUniqueEntryPath(
        string? directory,
        string fileNameNoExtension,
        string? extension,
        string uniqueSuffixSeparator = "")
    {
        var directoryPath = PaArchivePath.AsDirectoryOrRoot(directory);
        ArgumentNullException.ThrowIfNull(fileNameNoExtension);
        if (!IsSafeForEntryPathSegment(fileNameNoExtension))
        {
            throw new ArgumentException($"The {nameof(fileNameNoExtension)} must be safe for use as an entry path segment. Prevalidate using {nameof(TryMakeSafeForEntryPathSegment)} first.", nameof(fileNameNoExtension));
        }
        if (extension != null && !IsSafeForEntryPathSegment(extension))
        {
            throw new ArgumentException("The extension can be null, but cannot be empty or whitespace only, and must be a valid entry path segment.", nameof(directory));
        }

        var entryPathPrefix = $"{directoryPath.FullName}{fileNameNoExtension}";

        // First see if we can use the name as is
        var entryPath = $"{entryPathPrefix}{extension}";
        if (!ContainsEntry(entryPath))
            return entryPath;

        // If file with the same name already exists, add a number to the end of the name
        entryPathPrefix += uniqueSuffixSeparator;
        for (var i = 1; i < int.MaxValue; i++)
        {
            entryPath = $"{entryPathPrefix}{i}{extension}";
            if (!ContainsEntry(entryPath))
                return entryPath;
        }

        throw new InvalidOperationException("Failed to generate a unique name.");
    }

    private HeaderJson LoadHeader()
    {
        return GetRequiredEntry(MsappLayoutConstants.FileNames.Header)
            .DeserializeAsJson<HeaderJson>(MsappSerialization.HeaderJsonSerializeOptions);
    }

    /// <summary>
    /// Regular expression that matches any characters that are unsafe for entry filenames.<br/>
    /// Note: we don't allow any sort of directory separator chars for filenames to remove cross-platform issues.
    /// </summary>
    [GeneratedRegex("[^a-zA-Z0-9 ._-]")]
    private static partial Regex UnsafeFileNameCharactersRegex();

    /// <summary>
    /// Makes a user-provided name safe for use as an entry path segment in the archive.
    /// After making the name safe, it will be trimmed and empty strings will result in a false return value.<br/>
    /// Note: The set of allowed chars is currently more limited than what is actually allowed in a <see cref="PaArchivePath"/>.
    /// </summary>
    /// <param name="unsafeName">An unsafe name which may contain invalid chars for usage in an entry path segment (e.g. directory name or file name).</param>
    /// <param name="unsafeCharReplacementText">Unsafe characters in the name will be replaced with this string. Default is empty string.</param>
    /// <returns>true, when <paramref name="unsafeName"/> was converted to a safe, non-empty string; otherwise, false indicates that input could not be turned into a safe, non-empty string.</returns>
    public static bool TryMakeSafeForEntryPathSegment(
        string unsafeName,
        [NotNullWhen(true)]
        out string? safeName,
        string unsafeCharReplacementText = "")
    {
        ArgumentNullException.ThrowIfNull(unsafeName);
        ArgumentNullException.ThrowIfNull(unsafeCharReplacementText);

        safeName = UnsafeFileNameCharactersRegex()
            .Replace(unsafeName, unsafeCharReplacementText)
            .Trim()
            .EmptyToNull();

        return safeName != null;
    }

    /// <summary>
    /// Used to verify that a name is safe for use as a single path segment for an entry.
    /// Directory separator chars are not allowed in a path segment.<br/>
    /// Note: The set of allowed chars is currently more limited than what is actually allowed in a <see cref="PaArchivePath"/>.
    /// </summary>
    /// <param name="name">The proposed path segment name.</param>
    /// <returns>false when <paramref name="name"/> is null, empty, whitespace only, has leading or trailing whitespace, contains path separator chars or contains any other invalid chars.</returns>
    public static bool IsSafeForEntryPathSegment(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return !string.IsNullOrWhiteSpace(name)
            && !UnsafeFileNameCharactersRegex().IsMatch(name)
            && name.Trim().Length == name.Length; // No leading or trailing whitespace
    }
}
