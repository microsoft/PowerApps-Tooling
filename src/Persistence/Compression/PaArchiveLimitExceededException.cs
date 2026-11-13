// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Thrown when an archive opened with <see cref="PaArchiveLimits"/> exceeds one of the limits.
/// </summary>
public sealed class PaArchiveLimitExceededException : PersistenceLibraryException
{
    public PaArchiveLimitExceededException(PaArchiveLimit limit, PaArchiveStats stats, PaArchiveLimits limits)
        : base(PersistenceErrorCode.PaArchiveLimitExceeded, ComposeReason(limit, stats, limits))
    {
        Limit = limit;
        Stats = stats;
        Limits = limits;
    }

    /// <summary>
    /// The limit that was exceeded.
    /// </summary>
    public PaArchiveLimit Limit { get; }

    /// <summary>
    /// The stats of the archive.
    /// When the archive was rejected based on the entry count declared in its end of central directory record, before
    /// entries were read, only <see cref="PaArchiveStats.EntryCount"/> is set; the sizes are zero.
    /// </summary>
    public PaArchiveStats Stats { get; }

    /// <summary>
    /// The limits the archive was evaluated against.
    /// </summary>
    public PaArchiveLimits Limits { get; }

    private static string ComposeReason(PaArchiveLimit limit, PaArchiveStats stats, PaArchiveLimits limits)
    {
        if (limit == PaArchiveLimit.None)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "The limit must identify the limit that was exceeded.");
        ThrowIfNull(stats);
        ThrowIfNull(limits);

        var (actual, maximum) = limit switch
        {
            PaArchiveLimit.EntryCount => (FormatNumber(stats.EntryCount), FormatNumber(limits.MaxEntryCount)),
            PaArchiveLimit.CompressedSize => (FormatNumber(stats.CompressedSize), FormatNumber(limits.MaxCompressedSize)),
            PaArchiveLimit.ExpandedSize => (FormatNumber(stats.ExpandedSize), FormatNumber(limits.MaxExpandedSize)),
            PaArchiveLimit.EntrySize => (FormatNumber(stats.LargestEntrySize), FormatNumber(limits.MaxEntrySize)),
            PaArchiveLimit.TextEntrySize => (FormatNumber(stats.LargestTextEntrySize), FormatNumber(limits.MaxTextEntrySize)),
            PaArchiveLimit.CompressionRatio => (
                stats.CompressedSize <= 0 ? "unbounded" : FormatNumber((double)stats.ExpandedSize / stats.CompressedSize),
                FormatNumber(limits.MaxCompressionRatio)),
            _ => throw new ArgumentOutOfRangeException(nameof(limit), limit, null),
        };

        return string.Format(CultureInfo.InvariantCulture, "Limit: '{0}'; Actual: {1}; Maximum: {2};", limit, actual, maximum);

        static string FormatNumber(IFormattable value) => value.ToString(null, CultureInfo.InvariantCulture);
    }
}
