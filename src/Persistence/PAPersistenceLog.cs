// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal static partial class PAPersistenceLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Duplicate canonicalized entry found in zip archive, and will be ignored. EntryFullName: '{EntryFullName}'; CanonicalizedPath: '{CanonicalizedPath}';")]
    public static partial void LogDuplicateEntryIgnored(this ILogger<PaArchive> logger, string entryFullName, string canonicalizedPath);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Directory entries found in zip archives are ignored. EntryFullName: '{entryFullName}'; CanonicalizedPath: '{normalizedPath}';")]
    public static partial void LogDirectoryEntryIgnored(this ILogger<PaArchive> logger, string entryFullName, PaArchivePath normalizedPath);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "An entry found in zip archive has normalized path being to the root of the archive, which indicate an invalid entry, which is ignored. EntryFullName: '{entryFullName}';")]
    public static partial void LogNormalizedRootEntryIgnored(this ILogger<PaArchive> logger, string entryFullName);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "An entry found in zip archive has an invalid or malicious path and will be ignored. InvalidReason: '{invalidReason}'; EntryFullName: '{entryFullName}';")]
    public static partial void LogInvalidPathEntryIgnored(this ILogger<PaArchive> logger, string entryFullName, PaArchivePathInvalidReason invalidReason);
}
