// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal static partial class PAPersistenceLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Duplicate canonicalized entry found in zip archive, and will be ignored. EntryFullName: '{EntryFullName}'; CanonicalizedPath: '{CanonicalizedPath}';")]
    public static partial void LogDuplicateCanonicalizedEntryIgnored(this ILogger<MsappArchive> logger, string entryFullName, string canonicalizedPath);
}
