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
public class MsappArchive(
    Stream stream,
    ZipArchiveMode mode,
    bool leaveOpen = false,
    ILogger<MsappArchive>? logger = null)
    : PaArchive(stream, mode, leaveOpen, entryNameEncoding: null, logger)
{
    private HeaderJson? _header;
    private bool _packedJsonLoaded;
    private PackedJson? _packedJson;

    internal HeaderJson Header => _header ??= LoadHeader();

    /// <summary>
    /// The contents of the <c>packed.json</c> file, or <c>null</c> if the archive does not contain one.
    /// Loaded lazily on first access.
    /// </summary>
    public PackedJson? PackedJson => _packedJsonLoaded ? _packedJson : LoadPackedJson();

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
        ThrowIfNull(fileNameNoExtension);

        if (!PaArchivePath.IsValidSegment($"{fileNameNoExtension}{extension}"))
        {
            throw new ArgumentException($"The {nameof(fileNameNoExtension)} combined with {nameof(extension)} must be safe for use as an entry path segment. Prevalidate using {nameof(PaArchivePath)}.{nameof(PaArchivePath.TryMakeValidSegment)} first.", nameof(fileNameNoExtension));
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

    private PackedJson? LoadPackedJson()
    {
        if (TryGetEntry(MsappLayoutConstants.FileNames.Packed, out var entry))
            _packedJson = entry.DeserializeAsJson<PackedJson>(MsappSerialization.PackedJsonSerializeOptions);
        _packedJsonLoaded = true;
        return _packedJson;
    }
}
