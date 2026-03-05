// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Persistence.Tests;

public class FilePathComparer : IComparer<string>
{
    public static readonly FilePathComparer Instance = new();

    private FilePathComparer() { }

    public int Compare(string? x, string? y)
    {
        // Compare input strings as if they are file paths on Windows.
        // 1. case-insensitive
        // 2. treat both '/' and '\' as path separators (but don't ignore them entirely since they are important for determining directory structure)
        // 3. paths with separator chars indicate a sub-directory, which should be ordered before files in the same parent directory.
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // split into segments
        var xSegs = x.Split('/', '\\');
        var ySegs = y.Split('/', '\\');

        var minLen = Math.Min(xSegs.Length, ySegs.Length);
        for (var i = 0; i < minLen; i++)
        {
            // At each position, a segment that has more path components after it (a directory
            // name) is ordered before a segment that is the terminal name (a file).
            var xHasMore = i < xSegs.Length - 1;
            var yHasMore = i < ySegs.Length - 1;
            if (xHasMore != yHasMore)
                return xHasMore ? -1 : 1;  // directory-like path comes first

            var cmp = string.Compare(xSegs[i], ySegs[i], StringComparison.InvariantCultureIgnoreCase);
            if (cmp != 0)
                return cmp;
        }

        return xSegs.Length.CompareTo(ySegs.Length);
    }
}
