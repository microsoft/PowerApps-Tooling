// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal static partial class PAPersistenceLog
{
    [LoggerMessage(EventId = 101, Level = LogLevel.Warning,
        Message = "Duplicate normalized entry found in zip archive, and will be ignored. EntryFullName: '{entryFullName}'; NormalizedPath: '{normalizedPath}';")]
    public static partial void LogDuplicateEntryIgnored(this ILogger<PaArchive> logger, string entryFullName, string normalizedPath);

    [LoggerMessage(EventId = 102, Level = LogLevel.Warning,
        Message = "A directory entry with non-zero data length was found in zip archive. EntryFullName: '{entryFullName}'; NormalizedPath: '{normalizedPath}'; DataLength: {dataLength};")]
    public static partial void LogDirectoryEntryWithData(this ILogger<PaArchive> logger, string entryFullName, PaArchivePath normalizedPath, long dataLength);

    [LoggerMessage(EventId = 103, Level = LogLevel.Information,
        Message = "Directory entries found in zip archives are ignored. EntryFullName: '{entryFullName}'; NormalizedPath: '{normalizedPath}';")]
    public static partial void LogDirectoryEntryIgnored(this ILogger<PaArchive> logger, string entryFullName, PaArchivePath normalizedPath);

    [LoggerMessage(EventId = 104, Level = LogLevel.Information,
        Message = "An entry found in zip archive has normalized path being to the root of the archive, which indicate an invalid entry, which is ignored. EntryFullName: '{entryFullName}';")]
    public static partial void LogNormalizedRootEntryIgnored(this ILogger<PaArchive> logger, string entryFullName);

    [LoggerMessage(EventId = 105, Level = LogLevel.Warning,
        Message = "An entry found in zip archive has an invalid or malicious path and will be ignored. InvalidReason: '{invalidReason}'; EntryFullName: '{entryFullName}';")]
    public static partial void LogInvalidPathEntryIgnored(this ILogger<PaArchive> logger, string entryFullName, PaArchivePathInvalidReason invalidReason);

    [LoggerMessage(EventId = 106, Level = LogLevel.Error,
        Message = $"Extracting {nameof(PaArchiveEntry)} would have resulted in a file outside the specified destination directory. EntryFullName: '{{entryFullName}}'; NormalizedPath: '{{normalizedPath}}';")]
    public static partial void LogExtractingResultsInOutside(this ILogger<PaArchive> logger, string entryFullName, PaArchivePath normalizedPath);

    [LoggerMessage(EventId = 107, Level = LogLevel.Error,
        Message = "ZipArchive blocked on Net Framework due to an entry path containing invalid characters for the current OS. On Net Framework, this is a blocking condition when accessing any entries on the archive. Recommendation is to prefer .NET 8.0 or higher to get a better experience. Exception message: {exMessage}")]
    public static partial void LogZipArchiveBlockedDueToInvalidEntryPathCharsOnNetFramework(this ILogger<PaArchive> logger, string exMessage);


    [LoggerMessage(EventId = 211, Level = LogLevel.Debug,
        Message = "Entry types: {sourceCodeCount} source-code, {assetCount} asset, {headerCount} header, {otherCount} other entries.")]
    public static partial void LogUnpackInstructionsSummary(this ILogger<MsappPackingService> logger, int sourceCodeCount, int assetCount, int headerCount, int otherCount);
    [LoggerMessage(EventId = 212, Level = LogLevel.Information,
        Message = "Unpack complete. Extracted {extractedCount} files to disk. Wrote {referenceCount} reference entries to {msaprPath}.")]
    public static partial void LogUnpackComplete(this ILogger<MsappPackingService> logger, int extractedCount, int referenceCount, string msaprPath);
    [LoggerMessage(EventId = 221, Level = LogLevel.Warning,
        Message = "EnableLoadFromYaml is set to true, but the unpacked configuration does not indicate that PaYamlSourceCode was unpacked. Ignoring request to load from yaml.")]
    public static partial void LogPackEnableLoadFromYamlIgnored(this ILogger<MsappPackingService> logger);
    [LoggerMessage(EventId = 222, Level = LogLevel.Warning,
        Message = "Pack complete. Copied {copiedFromMsapr} entries from msapr. Added {addedFromDisk} files from disk. Output: {outputMsappPath}.")]
    public static partial void LogPackComplete(this ILogger<MsappPackingService> logger, int copiedFromMsapr, int addedFromDisk, string outputMsappPath);
}
