// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmAdapters;

/// <summary>
/// Polyfills for <see cref="System.IO.Path"/> static methods that are only available on .NET 5+.
/// Use these helpers instead of the BCL methods directly so that net48 call sites compile.
/// </summary>
public static class PathTfmAdapter
{
    /// <summary>
    /// Polyfill for <c>Path.EndsInDirectorySeparator</c> (.NET 5+).
    /// Returns true when <paramref name="path"/> ends with a directory separator character.
    /// </summary>
    public static bool EndsInDirectorySeparator(string path)
    {
#if NET5_0_OR_GREATER
        return Path.EndsInDirectorySeparator(path);
#else
        return path.Length > 0 &&
               (path[^1] == Path.DirectorySeparatorChar ||
                path[^1] == Path.AltDirectorySeparatorChar);
#endif
    }

    /// <summary>
    /// Polyfill for <c>Path.GetRelativePath</c> (.NET 5+).
    /// Returns a relative path from <paramref name="relativeTo"/> to <paramref name="path"/>.
    /// </summary>
    public static string GetRelativePath(string relativeTo, string path)
    {
#if NET5_0_OR_GREATER
        return Path.GetRelativePath(relativeTo, path);
#else
        var fullRelativeTo = Path.GetFullPath(relativeTo);
        var fullPath = Path.GetFullPath(path);

        // Append separator so the URI treats it as a directory, not a file
        if (fullRelativeTo[^1] != Path.DirectorySeparatorChar)
            fullRelativeTo += Path.DirectorySeparatorChar;

        var fromUri = new Uri(fullRelativeTo);
        var toUri = new Uri(fullPath);

        if (fromUri.Scheme != toUri.Scheme)
            return path; // cannot make relative across different schemes

        var relativeUri = fromUri.MakeRelativeUri(toUri);
        return Uri.UnescapeDataString(relativeUri.ToString())
            .Replace('/', Path.DirectorySeparatorChar);
#endif
    }
}
