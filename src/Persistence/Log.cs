// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "{message}")]
    public static partial void InfoMessage(this ILogger logger, string message);

    [LoggerMessage(EventId = 2000, Level = LogLevel.Error, Message = "Duplicate entry found in archive: {entryFullName}")]
    public static partial void DuplicateEntry(this ILogger logger, string entryFullName);
}
