// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Optional limits on the sizes and entry count of a zip archive, which callers can use to bound the resources used when
/// opening archives. Values are taken from the archive's central directory, before any entry is read.
/// </summary>
/// <remarks>
/// A value is over a limit when it is greater than the limit; a value equal to the limit is allowed.
/// </remarks>
public sealed record PaArchiveLimits
{
    /// <summary>
    /// A recommended set of limits. Limits are only applied when a caller passes them in; this instance is not applied by default.
    /// </summary>
    public static PaArchiveLimits Default { get; } = new()
    {
        MaxExpandedSize = 4L * 1024 * 1024 * 1024,
        MaxCompressedSize = 1L * 1024 * 1024 * 1024,
        MaxCompressionRatio = 250,
        RatioCheckMinExpandedSize = 100L * 1024 * 1024,
        MaxEntrySize = 512L * 1024 * 1024,
        MaxEntryCount = 10_000,
        MaxTextEntrySize = 256L * 1024 * 1024,
    };

    /// <summary>
    /// The maximum total uncompressed size, in bytes, of all entries.
    /// </summary>
    public required long MaxExpandedSize { get; init; }

    /// <summary>
    /// The maximum total compressed size, in bytes, of all entries.
    /// </summary>
    public required long MaxCompressedSize { get; init; }

    /// <summary>
    /// The maximum ratio of total uncompressed size to total compressed size.
    /// Only checked when the total uncompressed size is greater than <see cref="RatioCheckMinExpandedSize"/>.
    /// </summary>
    public required double MaxCompressionRatio { get; init; }

    /// <summary>
    /// The total uncompressed size, in bytes, that must be exceeded before <see cref="MaxCompressionRatio"/> is checked.
    /// </summary>
    public required long RatioCheckMinExpandedSize { get; init; }

    /// <summary>
    /// The maximum uncompressed size, in bytes, of any single entry.
    /// </summary>
    public required long MaxEntrySize { get; init; }

    /// <summary>
    /// The maximum number of entries, including entries that <see cref="PaArchive"/> ignores (e.g. directory entries).
    /// </summary>
    public required int MaxEntryCount { get; init; }

    /// <summary>
    /// The maximum uncompressed size, in bytes, of any single text entry.
    /// A text entry is one whose path ends with <c>.json</c>, <c>.yaml</c>, <c>.yml</c>, <c>.xml</c> or <c>.txt</c> (case-insensitive).
    /// </summary>
    public required long MaxTextEntrySize { get; init; }

    /// <summary>
    /// Evaluates the stats of an archive against these limits.
    /// </summary>
    /// <returns>
    /// The first limit exceeded, checked in this order: <see cref="PaArchiveLimit.EntryCount"/>, <see cref="PaArchiveLimit.CompressedSize"/>,
    /// <see cref="PaArchiveLimit.ExpandedSize"/>, <see cref="PaArchiveLimit.EntrySize"/>, <see cref="PaArchiveLimit.TextEntrySize"/>,
    /// <see cref="PaArchiveLimit.CompressionRatio"/>. Returns <see cref="PaArchiveLimit.None"/> when no limit is exceeded.
    /// </returns>
    public PaArchiveLimit Evaluate(PaArchiveStats stats)
    {
        ThrowIfNull(stats);

        if (stats.EntryCount > MaxEntryCount)
            return PaArchiveLimit.EntryCount;
        if (stats.CompressedSize > MaxCompressedSize)
            return PaArchiveLimit.CompressedSize;
        if (stats.ExpandedSize > MaxExpandedSize)
            return PaArchiveLimit.ExpandedSize;
        if (stats.LargestEntrySize > MaxEntrySize)
            return PaArchiveLimit.EntrySize;
        if (stats.LargestTextEntrySize > MaxTextEntrySize)
            return PaArchiveLimit.TextEntrySize;
        if (stats.ExpandedSize > RatioCheckMinExpandedSize
            && (stats.CompressedSize <= 0 || (double)stats.ExpandedSize / stats.CompressedSize > MaxCompressionRatio))
            return PaArchiveLimit.CompressionRatio;

        return PaArchiveLimit.None;
    }
}
