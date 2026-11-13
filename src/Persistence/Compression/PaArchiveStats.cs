// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Sizes and entry count of a zip archive, as recorded in its central directory.
/// </summary>
/// <param name="CompressedSize">Total compressed size, in bytes, of all entries.</param>
/// <param name="ExpandedSize">Total uncompressed size, in bytes, of all entries.</param>
/// <param name="EntryCount">The number of entries.</param>
/// <param name="LargestEntrySize">The uncompressed size, in bytes, of the largest entry.</param>
/// <param name="LargestTextEntrySize">The uncompressed size, in bytes, of the largest text entry. See <see cref="PaArchiveLimits.MaxTextEntrySize"/>.</param>
public sealed record PaArchiveStats(long CompressedSize, long ExpandedSize, int EntryCount, long LargestEntrySize, long LargestTextEntrySize)
{
    private static readonly string[] TextEntryExtensions = [".json", ".yaml", ".yml", ".xml", ".txt"];

    /// <summary>
    /// Computes stats from the given entries.
    /// </summary>
    /// <remarks>
    /// Totals and the entry count stop at their maximum values instead of overflowing.
    /// A negative size is treated as <see cref="long.MaxValue"/>.
    /// </remarks>
    /// <param name="entries">The entries, where FullName is the entry's path in the archive, Length its uncompressed size and CompressedLength its compressed size.</param>
    public static PaArchiveStats FromEntries(IEnumerable<(string FullName, long Length, long CompressedLength)> entries)
    {
        ThrowIfNull(entries);

        long compressedSize = 0;
        long expandedSize = 0;
        var entryCount = 0;
        long largestEntrySize = 0;
        long largestTextEntrySize = 0;

        foreach (var (fullName, length, compressedLength) in entries)
        {
            var entrySize = length < 0 ? long.MaxValue : length;
            var entryCompressedSize = compressedLength < 0 ? long.MaxValue : compressedLength;

            if (entryCount < int.MaxValue)
                entryCount++;
            compressedSize = AddWithoutOverflow(compressedSize, entryCompressedSize);
            expandedSize = AddWithoutOverflow(expandedSize, entrySize);
            largestEntrySize = Math.Max(largestEntrySize, entrySize);
            if (IsTextEntry(fullName))
                largestTextEntrySize = Math.Max(largestTextEntrySize, entrySize);
        }

        return new(compressedSize, expandedSize, entryCount, largestEntrySize, largestTextEntrySize);
    }

    internal static bool IsTextEntry(string? fullName)
    {
        if (fullName is null)
            return false;

        foreach (var extension in TextEntryExtensions)
        {
            if (fullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static long AddWithoutOverflow(long total, long value)
    {
        Debug.Assert(total >= 0 && value >= 0);
        return total > long.MaxValue - value ? long.MaxValue : total + value;
    }
}
