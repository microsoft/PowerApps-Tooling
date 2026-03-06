// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Serialization;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// A service for packing and unpacking .msapp files.
/// </summary>
public sealed class MsappPackingService(
    IMsappArchiveFactory _msappFactory,
    MsappReferenceArchiveFactory _msappReferenceFactory,
    ILogger<MsappPackingService>? _logger = null)
{
    /// <summary>
    /// The minimum <see cref="MsappArchive.MSAppStructureVersion"/> required to unpack an msapp.
    /// "2.4.0" is the min version where PaYaml sources are saved, which is required for the ALM scenarios.
    /// </summary>
    public static readonly Version MinSupportedMSAppStructureVersion = new(2, 4, 0);

    /// <summary>
    /// The minimum <see cref="MsappArchive.DocVersion"/> required to unpack an msapp.
    /// </summary>
    public static readonly Version MinSupportedDocVersion = new(1, 348);

    public void UnpackToDirectory(
        string msappPath,
        string outputDirectory,
        bool overwriteOutput = false,
        UnpackedConfiguration? unpackedConfig = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(msappPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        unpackedConfig ??= new();
        if (!unpackedConfig.ContentTypes.Any())
            throw new ArgumentException($"{nameof(unpackedConfig)}.{nameof(unpackedConfig.ContentTypes)} should not be empty", nameof(unpackedConfig));

        if (outputDirectory != Path.GetFullPath(outputDirectory))
            throw new ArgumentException($"{nameof(outputDirectory)} should be an absolute path.", nameof(outputDirectory));

        var outputDirectoryWithTrailingSlash = outputDirectory;
        if (!Path.EndsInDirectorySeparator(outputDirectoryWithTrailingSlash))
            outputDirectoryWithTrailingSlash += Path.DirectorySeparatorChar;

        // Step 1: compute output paths
        var msaprPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(msappPath) + MsaprLayoutConstants.FileExtensions.Msapr);
        var assetsOutputDirectoryPath = Path.Combine(outputDirectory, MsappLayoutConstants.DirectoryNames.Assets);
        var srcOutputDirectoryPath = Path.Combine(outputDirectory, MsappLayoutConstants.DirectoryNames.Src);

        // Step 2: check for conflicts with existing files/folders
        // Note: This logic doesn't require inspecting the msapp first, as the top-level output files/folders are replaced wholesale
        if (!overwriteOutput)
        {
            if (File.Exists(msaprPath))
                throw new MsappUnpackException($"Output file '{msaprPath}' already exists and overwriting output is not enabled.");

            // Source code should all go into the same 'Src' folder, so we only need to see if it exists and has files inside
            // We will silently remove any empty 'Src' folder
            if (Directory.Exists(srcOutputDirectoryPath)
                && Directory.EnumerateFiles(srcOutputDirectoryPath, "*", SearchOption.AllDirectories).Any())
            {
                throw new MsappUnpackException($"Output folder '{srcOutputDirectoryPath}' is not empty and overwriting output is not enabled.");
            }

            // Assets should all go into the same 'Assets' folder, so we only need to see if it exists and has files inside
            // We will silently remove any empty 'Assets' folder
            if (Directory.Exists(assetsOutputDirectoryPath)
                && Directory.EnumerateFiles(assetsOutputDirectoryPath, "*", SearchOption.AllDirectories).Any())
            {
                throw new MsappUnpackException($"Output folder '{assetsOutputDirectoryPath}' is not empty and overwriting output is not enabled.");
            }
        }

        // Step 3: open source archive and build manifest
        using var sourceArchive = _msappFactory.Open(msappPath);

        ValidateMsappUnpackIsSupported(sourceArchive);

        var entryInstructions = BuildUnpackInstructions(sourceArchive, unpackedConfig);
        _logger?.LogDebug(
            "Entry types: {SourceCode} source-code, {Asset} asset, {Header} header, {Other} other entries.",
            entryInstructions.Count(e => e.ContentType == MsappContentType.PaYamlSourceCode),
            entryInstructions.Count(e => e.ContentType == MsappContentType.Asset),
            entryInstructions.Count(e => e.ContentType == MsappContentType.Header),
            entryInstructions.Count(e => e.ContentType == MsappContentType.Other));

        // Step 4: Clear existing output folders (even if the content type for the folder isn't being unpacked)
        if (Directory.Exists(srcOutputDirectoryPath))
            Directory.Delete(srcOutputDirectoryPath, recursive: true);
        if (Directory.Exists(assetsOutputDirectoryPath))
            Directory.Delete(assetsOutputDirectoryPath, recursive: true);

        // Create/overwite .msapr
        Directory.CreateDirectory(outputDirectory);
        using var msaprArchive = _msappReferenceFactory.CreateNew(msaprPath, CreateMsaprHeaderJson(unpackedConfig), overwrite: overwriteOutput);

        // Perform unpack instructions on msapp entries
        var extractedCount = 0;
        var referenceCount = 0;
        foreach (var entryInstruction in entryInstructions)
        {
            if (entryInstruction.UnpackToRelativePath is not null)
            {
                var targetPath = Path.GetFullPath(Path.Combine(outputDirectory, entryInstruction.UnpackToRelativePath));
                // REVIEW: the ZipArchiveEntry.FullName docs example indicates we should ensure the targetPath is actually still under the output path.
                //   This could be that the relative dest path was maliciously formed.
                if (!targetPath.StartsWith(outputDirectoryWithTrailingSlash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Malicious msapp entry path found with FullName '{entryInstruction.MsappEntry.FullName}'.");

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                entryInstruction.MsappEntry.ExtractToFile(targetPath);

                extractedCount++;
            }
            else if (entryInstruction.CopyToMsaprEntryPath is not null)
            {
                msaprArchive.AddEntryFrom(entryInstruction.CopyToMsaprEntryPath, entryInstruction.MsappEntry);
                referenceCount++;
            }
        }

        _logger?.LogInformation(
            "Unpack complete. Extracted {Extracted} files to disk. Wrote {Reference} reference entries to {MsaprPath}.",
            extractedCount, referenceCount, msaprPath);
    }

    internal static void ValidateMsappUnpackIsSupported(MsappArchive msappArchive)
    {
        if (msappArchive.MSAppStructureVersion < MinSupportedMSAppStructureVersion)
            throw new MsappUnpackException($"MSAppStructureVersion {msappArchive.MSAppStructureVersion} is below the minimum supported version {MinSupportedMSAppStructureVersion}.");

        if (msappArchive.DocVersion < MinSupportedDocVersion)
            throw new MsappUnpackException($"DocVersion {msappArchive.DocVersion} is below the minimum supported version {MinSupportedDocVersion}.");
    }

    private static MsaprHeaderJson CreateMsaprHeaderJson(UnpackedConfiguration unpackedConfig)
    {
        return new()
        {
            MsaprStructureVersion = MsaprHeaderJson.CurrentMsaprStructureVersion,
            UnpackedConfiguration = new()
            {
                ContentTypes = [.. unpackedConfig.ContentTypes.Select(v => v.ToString())],
            },
        };
    }

    /// <summary>
    /// Inspects every entry in <paramref name="sourceArchive"/> and returns a manifest
    /// describing each entry's category and planned output path.
    /// Does not access the file system.
    /// </summary>
    internal static IEnumerable<MsappUnpackEntryInstruction> BuildUnpackInstructions(MsappArchive sourceArchive, UnpackedConfiguration options)
    {
        ArgumentNullException.ThrowIfNull(sourceArchive);
        options ??= new UnpackedConfiguration();

        if (options.EnablesContentType(MsappUnpackableContentType.Assets))
        {
            // Design Notes:
            // In order to support unpacking of Assets, we're going to need to understand what kind of additional metadata we'll need
            // Assets in msapp are registered in Resources.json, and we'll need to make sure we only extract files which have valid metadata there.
            // We'll also likely need to consider changing the file names from guid.jpg to something human readable, like the entity Name (with appropriate file character sanitization).
            // And we'll need to keep track of our mapping from entity to file name in the msapr in a new metadata file, so that we can reverse the process when packing back into a msapp.
            // Also, if we're packing the file back up, then would we need to ensure we invalidate the 'RootPath' in blob storage during load?
            // Alternatively, as a starting iterative solution, we could first track the hash of the files, and use that to detect if an asset was modified, and throw error that it's not yet supported.
            throw new NotImplementedException();
        }

        foreach (var entry in sourceArchive.Entries)
        {
            var contentType = GetContentType(entry.NormalizedPath);
            if (contentType is MsappContentType.PaYamlSourceCode && options.EnablesContentType(MsappUnpackableContentType.PaYamlSourceCode))
            {
                yield return new(entry, contentType)
                {
                    UnpackToRelativePath = entry.FullName,
                };
            }
            else
            {
                // Default to copy entry into msapr as is under the 'msapp' folder
                yield return new(entry, contentType)
                {
                    CopyToMsaprEntryPath = MsappDirPath.Combine(entry.NormalizedPath),
                };
            }
        }
    }

    private static readonly PaArchivePath SrcDirPath = PaArchivePath.AsDirectoryOrRoot(MsappLayoutConstants.DirectoryNames.Src);
    private static readonly PaArchivePath AssetsDirPath = PaArchivePath.AsDirectoryOrRoot(MsappLayoutConstants.DirectoryNames.Assets);
    private static readonly PaArchivePath MsappDirPath = PaArchivePath.AsDirectoryOrRoot(MsaprLayoutConstants.DirectoryNames.Msapp);

    private static MsappContentType GetContentType(PaArchivePath entryPath)
    {
        if (SrcDirPath.ContainsPath(entryPath))
        {
            // In case there are other files inside the app which are not actual yaml files
            if (entryPath.MatchesFileExtension(MsappLayoutConstants.FileExtensions.PaYaml))
            {
                return MsappContentType.PaYamlSourceCode;
            }
        }
        else if (AssetsDirPath.ContainsPath(entryPath))
        {
            return MsappContentType.Asset;
        }
        else if (entryPath.Equals(MsappLayoutConstants.FileNames.Header))
        {
            return MsappContentType.Header;
        }

        return MsappContentType.Other;
    }

    /// <summary>
    /// Packs an msapp given the path to the msapr file.
    /// The contents of the folder where the msapr file resides are inspected to be included in the msapp.
    /// </summary>
    /// <param name="packingClient">Information about the client performing the packing.</param>
    /// <param name="overwriteOutput">Indicates whether to allow overwriting the output if it already exists.</param>
    public void PackFromMsappReferenceFile(
        string msaprPath,
        string outputMsappPath,
        PackedJsonPackingClient? packingClient = null,
        bool overwriteOutput = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(msaprPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputMsappPath);

        msaprPath = Path.GetFullPath(msaprPath);

        if (!overwriteOutput && File.Exists(outputMsappPath))
            throw new MsappPackException($"Output file '{outputMsappPath}' already exists and overwriting output is not enabled.");

        var unpackedFolderPath = Path.GetDirectoryName(msaprPath)!;

        using var msaprArchive = _msappReferenceFactory.Open(msaprPath);
        var unpackedConfig = ParseUnpackedConfiguration(msaprArchive.Header);

        // Materialize instructions before creating the output file so any errors (e.g. unsupported src files) are raised first.
        var packInstructions = BuildPackInstructions(msaprArchive, unpackedFolderPath, unpackedConfig, _logger).ToList();

        using var outputMsapp = _msappFactory.Create(outputMsappPath, overwrite: overwriteOutput);

        var packedJsonPath = new PaArchivePath(MsappLayoutConstants.FileNames.Packed);
        var copiedFromMsaprCount = 0;
        var addedFromDiskCount = 0;
        foreach (var instruction in packInstructions)
        {
            // packed.json is always freshly generated below; skip any prior version from the msapr
            if (instruction.MsappEntryPath.Equals(packedJsonPath))
                continue;

            if (instruction.CopyFromMsaprEntry is not null)
            {
                outputMsapp.AddEntryFrom(instruction.MsappEntryPath, instruction.CopyFromMsaprEntry);
                copiedFromMsaprCount++;
            }
            else if (instruction.ReadFromFilePath is not null)
            {
                var newEntry = outputMsapp.CreateEntry(instruction.MsappEntryPath);
                using var srcStream = File.OpenRead(instruction.ReadFromFilePath);
                using var destStream = newEntry.Open();
                srcStream.CopyTo(destStream);
                addedFromDiskCount++;
            }
        }

        outputMsapp.AddEntryFromJson(MsappLayoutConstants.FileNames.Packed, new PackedJson
        {
            PackedStructureVersion = PackedJson.CurrentPackedStructureVersion,
            LastPackedDateTimeUtc = DateTime.UtcNow,
            PackingClient = packingClient,
            LoadConfiguration = new PackedJsonLoadConfiguration
            {
                LoadFromYaml = unpackedConfig.EnablesContentType(MsappUnpackableContentType.PaYamlSourceCode),
            },
        }, MsappSerialization.PackedJsonSerializeOptions);

        _logger?.LogInformation(
            "Pack complete. Copied {CopiedFromMsapr} entries from msapr. Added {AddedFromDisk} files from disk. Output: {OutputMsappPath}.",
            copiedFromMsaprCount, addedFromDiskCount, outputMsappPath);
    }

    private static UnpackedConfiguration ParseUnpackedConfiguration(MsaprHeaderJson header)
    {
        var contentTypes = header.UnpackedConfiguration.ContentTypes
            .Select(s => Enum.TryParse<MsappUnpackableContentType>(s, out var v) ? v : (MsappUnpackableContentType?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToImmutableArray();
        return new UnpackedConfiguration { ContentTypes = contentTypes };
    }

    /// <summary>
    /// Builds the list of instructions for packing a .msapr and its sibling unpacked files into a .msapp.
    /// Does not access the file system except to enumerate files under the Src directory.
    /// </summary>
    internal static IEnumerable<MsappPackEntryInstruction> BuildPackInstructions(
        MsappReferenceArchive msaprArchive,
        string unpackedFolderPath,
        UnpackedConfiguration unpackedConfig,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(msaprArchive);
        ArgumentNullException.ThrowIfNull(unpackedFolderPath);
        unpackedConfig ??= new UnpackedConfiguration();

        // Yield entries stored in the msapr (strip the leading "msapp/" prefix)
        foreach (var entry in msaprArchive.GetEntriesInDirectory(MsappDirPath, recursive: true))
        {
            var msappEntryPath = new PaArchivePath(entry.FullName[MsappDirPath.FullName.Length..]);
            yield return new(msappEntryPath)
            {
                CopyFromMsaprEntry = entry
            };
        }

        // Yield entries from disk (Src/**/*.pa.yaml files)
        if (unpackedConfig.EnablesContentType(MsappUnpackableContentType.PaYamlSourceCode))
        {
            var srcDir = Path.Combine(unpackedFolderPath, MsappLayoutConstants.DirectoryNames.Src);
            if (Directory.Exists(srcDir))
            {
                foreach (var filePath in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
                {
                    if (filePath.EndsWith(MsappLayoutConstants.FileExtensions.PaYaml, StringComparison.OrdinalIgnoreCase))
                    {
                        var relPath = Path.GetRelativePath(unpackedFolderPath, filePath);
                        yield return new(new PaArchivePath(relPath))
                        {
                            ReadFromFilePath = filePath
                        };
                    }
                    else
                    {
                        logger?.LogWarning("Unsupported source file will not be included in the msapp: '{FilePath}'", filePath);
                    }
                }
            }
        }

        if (unpackedConfig.EnablesContentType(MsappUnpackableContentType.Assets))
        {
            throw new NotImplementedException();
        }
    }
}

internal record MsappUnpackEntryInstruction(PaArchiveEntry MsappEntry, MsappContentType ContentType)
{
    public string? UnpackToRelativePath { get; init; }
    public PaArchivePath? CopyToMsaprEntryPath { get; init; }
}

internal record MsappPackEntryInstruction(PaArchivePath MsappEntryPath)
{
    /// <summary>Disk path to read from when packing from an extracted file.</summary>
    public string? ReadFromFilePath { get; init; }

    /// <summary>msapr archive entry to copy directly into the output msapp.</summary>
    public PaArchiveEntry? CopyFromMsaprEntry { get; init; }
}
