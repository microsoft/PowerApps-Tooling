// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

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
}
