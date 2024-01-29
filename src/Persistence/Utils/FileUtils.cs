// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Utils;

public static class FileUtils
{
    /// <summary>
    /// Converts backslashes to forward slashes, removes trailing slashes, and converts to lowercase.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string NormalizePath(string path)
    {
        return path.Trim().Replace('\\', '/').Trim('/').ToLowerInvariant();
    }
}
