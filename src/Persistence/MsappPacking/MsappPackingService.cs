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

    public async Task UnpackToDirectoryAsync(
        string msappPath,
        string outputDirectory,
        MsappUnpackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(msappPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        options ??= new();

        if (!options.UnpackedConfig.ContentTypes.Any())
            throw new ArgumentException($"{nameof(options)}.{nameof(options.UnpackedConfig)}.{nameof(options.UnpackedConfig.ContentTypes)} should not be empty", nameof(options));

        if (outputDirectory != Path.GetFullPath(outputDirectory))
            throw new ArgumentException($"{nameof(outputDirectory)} should be an absolute path.", nameof(outputDirectory));

        var outputDirectoryWithTrailingSlash = outputDirectory;
        if (!Path.EndsInDirectorySeparator(outputDirectoryWithTrailingSlash))
            outputDirectoryWithTrailingSlash += Path.DirectorySeparatorChar;

        // Step 1: compute output paths
        var msaprPath = Path.Combine(outputDirectory, (options.MsaprName ?? Path.GetFileNameWithoutExtension(msappPath)) + MsaprLayoutConstants.FileExtensions.Msapr);
        var assetsOutputDirectoryPath = Path.Combine(outputDirectory, MsappLayoutConstants.DirectoryNames.Assets);
        var srcOutputDirectoryPath = Path.Combine(outputDirectory, MsappLayoutConstants.DirectoryNames.Src);

        // Step 2: check for conflicts with existing files/folders
        // Note: This logic doesn't require inspecting the msapp first, as the top-level output files/folders are replaced wholesale
        if (!options.OverwriteOutput)
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

        var entryInstructions = BuildUnpackInstructions(sourceArchive, options.UnpackedConfig);
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
        using var msaprArchive = await _msappReferenceFactory.CreateNewAsync(msaprPath, CreateMsaprHeaderJson(options.UnpackedConfig), overwrite: options.OverwriteOutput, cancellationToken).ConfigureAwait(false);

        // Perform unpack instructions on msapp entries
        var extractedCount = 0;
        var referenceCount = 0;
        foreach (var entryInstruction in entryInstructions)
        {
            if (entryInstruction.InstructionType is MsappUnpackInstructionType.UnpackToRelativeDirectory)
            {
                await entryInstruction.MsappEntry.ExtractRelativeToDirectoryAsync(outputDirectory, overwrite: options.OverwriteOutput, cancellationToken).ConfigureAwait(false);
                extractedCount++;
            }
            else if (entryInstruction.InstructionType is MsappUnpackInstructionType.CopyToMsapr)
            {
                Debug.Assert(entryInstruction.CopyToMsaprEntryPath is not null);
                await msaprArchive.AddEntryFromAsync(entryInstruction.CopyToMsaprEntryPath, entryInstruction.MsappEntry, cancellationToken).ConfigureAwait(false);
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
                yield return new(entry, contentType, MsappUnpackInstructionType.UnpackToRelativeDirectory);
            }
            else
            {
                // Default to copy entry into msapr as is under the 'msapp' folder
                yield return new(entry, contentType, MsappUnpackInstructionType.CopyToMsapr)
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
    public async Task PackFromMsappReferenceFileAsync(
        string msaprPath,
        string outputMsappPath,
        PackedJsonPackingClient packingClient,
        MsappPackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(msaprPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputMsappPath);
        options ??= new();

        msaprPath = Path.GetFullPath(msaprPath);

        if (!options.OverwriteOutput && File.Exists(outputMsappPath))
            throw new MsappPackException($"Output file '{outputMsappPath}' already exists and overwriting output is not enabled.");

        var unpackedFolderPath = Path.GetDirectoryName(msaprPath)!;

        using var msaprArchive = _msappReferenceFactory.Open(msaprPath);
        var unpackedConfig = ParseUnpackedConfiguration(msaprArchive.Header);

        // Materialize instructions before creating the output file so any errors (e.g. unsupported src files) are raised first.
        var packInstructions = BuildPackInstructions(msaprArchive, unpackedFolderPath, unpackedConfig, _logger).ToList();

        using var outputMsapp = _msappFactory.Create(outputMsappPath, overwrite: options.OverwriteOutput);

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
                await outputMsapp.AddEntryFromAsync(instruction.MsappEntryPath, instruction.CopyFromMsaprEntry, cancellationToken).ConfigureAwait(false);
                copiedFromMsaprCount++;
            }
            else if (instruction.ReadFromFilePath is not null)
            {
                var newEntry = outputMsapp.CreateEntry(instruction.MsappEntryPath);
                using var srcStream = File.OpenRead(instruction.ReadFromFilePath);
                using var destStream = await newEntry.OpenAsync(cancellationToken).ConfigureAwait(false);
                await srcStream.CopyToAsync(destStream, cancellationToken).ConfigureAwait(false);
                addedFromDiskCount++;
            }
        }

        if (options.EnableLoadFromYaml && !unpackedConfig.EnablesContentType(MsappUnpackableContentType.PaYamlSourceCode))
        {
            _logger?.LogWarning("EnableLoadFromYaml is set to true, but the unpacked configuration does not indicate that PaYamlSourceCode was unpacked. Ignoring request to load from yaml.");
            options = options with { EnableLoadFromYaml = false };
        }

        await outputMsapp.AddEntryFromJsonAsync(
            MsappLayoutConstants.FileNames.Packed,
            new PackedJson
            {
                PackedStructureVersion = PackedJson.CurrentPackedStructureVersion,
                LastPackedDateTimeUtc = DateTime.UtcNow,
                PackingClient = packingClient,
                LoadConfiguration = new()
                {
                    LoadFromYaml = options.EnableLoadFromYaml,
                },
            },
            MsappSerialization.PackedJsonSerializeOptions,
            cancellationToken).ConfigureAwait(false);

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

internal enum MsappUnpackInstructionType
{
    UnpackToRelativeDirectory,
    CopyToMsapr
}

internal record MsappUnpackEntryInstruction(PaArchiveEntry MsappEntry, MsappContentType ContentType, MsappUnpackInstructionType InstructionType)
{
    public PaArchivePath? CopyToMsaprEntryPath { get; init; }
}

internal record MsappPackEntryInstruction(PaArchivePath MsappEntryPath)
{
    /// <summary>Disk path to read from when packing from an extracted file.</summary>
    public string? ReadFromFilePath { get; init; }

    /// <summary>msapr archive entry to copy directly into the output msapp.</summary>
    public PaArchiveEntry? CopyFromMsaprEntry { get; init; }
}
