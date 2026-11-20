// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

public partial class PaArchive
{
    /// <summary>
    /// The limits this archive was opened with, or null when it was opened without limits.
    /// </summary>
    public PaArchiveLimits? Limits { get; }

    /// <summary>
    /// Computes stats from the sizes recorded for all entries in the archive's central directory, without reading any entry.
    /// This includes entries that are not available via <see cref="Entries"/>, such as directory entries, duplicate entries and
    /// entries with invalid paths.
    /// Can be used whether or not the archive was opened with <see cref="Limits"/>; e.g. with <see cref="PaArchiveLimits.Evaluate(PaArchiveStats)"/>.
    /// </summary>
    /// <remarks>
    /// In <see cref="ZipArchiveMode.Update"/> mode, the sizes are only valid before entries are opened, the same as <see cref="ZipArchiveEntry.Length"/>.
    /// </remarks>
    /// <exception cref="NotSupportedException">The archive is in <see cref="ZipArchiveMode.Create"/> mode.</exception>
    public PaArchiveStats GetStats()
    {
        ThrowIfDisposed();
        if (Mode == ZipArchiveMode.Create)
            throw new NotSupportedException($"{nameof(GetStats)} is not supported in {nameof(ZipArchiveMode)}.{nameof(ZipArchiveMode.Create)} mode.");

        return ComputeStats(InnerZipArchive, _logger);
    }

    private static PaArchiveStats ComputeStats(ZipArchive zipArchive, ILogger<PaArchive>? logger)
    {
        return PaArchiveStats.FromEntries(GetZipEntries(zipArchive, logger).Select(e => (e.FullName, e.Length, e.CompressedLength)));
    }

    /// <summary>
    /// Gets the entries of the zip archive, reading its central directory if not already read.
    /// </summary>
    private static ReadOnlyCollection<ZipArchiveEntry> GetZipEntries(ZipArchive zipArchive, ILogger<PaArchive>? logger)
    {
#if NETFRAMEWORK
        // On .NET Framework 4.8, ZipArchive.Entries lazily reads the central directory.
        // If any entry has a name containing a char in Path.GetInvalidPathChars() (e.g. '|'),
        // the first access throws ArgumentException and NO entries can be enumerated at all.
        // Checking here, before callers enumerate the entries, means they don't have to reason about
        // ArgumentException originating from the various other calls they make.
        // The lazy initialization of FullPath for .NET implementations is more lenient on path chars,
        // allowing better usability, and pushing the validation to other APIs.
        try
        {
            return zipArchive.Entries;
        }
        catch (ArgumentException ex) when (ex.StackTrace.Contains("Path.CheckInvalidPathChars"))
        {
            logger?.LogZipArchiveBlockedDueToInvalidEntryPathCharsOnNetFramework(ex.Message);
            throw new PersistenceLibraryException(PersistenceErrorCode.ZipArchiveBlockedDueToInvalidEntryPathCharsOnNetFramework, "The zip archive contains at least one entry with illegal path characters and cannot be loaded on Net Framework. Better support is available when using .NET 8.0 or later.", ex);
        }
#else
        return zipArchive.Entries;
#endif
    }

    private static ZipArchive OpenZipArchiveWithLimits(
        Stream stream,
        ZipArchiveMode mode,
        bool leaveOpen,
        Encoding? entryNameEncoding,
        PaArchiveLimits limits,
        ILogger<PaArchive>? logger)
    {
        ThrowIfNull(stream);
        Debug.Assert(mode is ZipArchiveMode.Read or ZipArchiveMode.Update, "Archives in Create mode are not checked.");

        // Let ZipArchive report streams that don't support Update mode as it normally would.
        // An empty stream in Update mode is a new archive, so there's nothing to check.
        if (mode == ZipArchiveMode.Update && (!stream.CanRead || !stream.CanWrite || !stream.CanSeek || stream.Length == 0))
            return new ZipArchive(stream, mode, leaveOpen, entryNameEncoding);

        ZipArchive? zipArchive = null;
        try
        {
            ThrowIfDeclaredEntryCountExceeded(stream, limits, logger);

            if (mode == ZipArchiveMode.Read)
            {
                zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen, entryNameEncoding);
                ThrowIfLimitExceeded(zipArchive, limits, logger);
                return zipArchive;
            }

            // ZipArchive rewrites the archive when it's disposed in Update mode. Check the limits using a separate read-only
            // instance first, so an archive that exceeds them is never opened in Update mode and is left unchanged.
            var startPosition = stream.Position;
            using (var readOnlyZipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding))
            {
                ThrowIfLimitExceeded(readOnlyZipArchive, limits, logger);
            }

            stream.Position = startPosition;
            return new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen, entryNameEncoding);
        }
        catch
        {
            // The caller won't get an instance to dispose, so release what was opened here.
            // Disposing a ZipArchive in Read mode doesn't write to the stream.
            zipArchive?.Dispose();
            if (!leaveOpen)
                stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// When the stream is seekable, checks the entry count declared in the archive's end of central directory record, before
    /// <see cref="ZipArchive"/> reads the central directory.
    /// </summary>
    private static void ThrowIfDeclaredEntryCountExceeded(Stream stream, PaArchiveLimits limits, ILogger<PaArchive>? logger)
    {
        var declaredEntryCount = ZipEndOfCentralDirectoryReader.TryReadDeclaredEntryCount(stream);
        if (declaredEntryCount > limits.MaxEntryCount)
        {
            var stats = new PaArchiveStats(
                CompressedSize: 0,
                ExpandedSize: 0,
                EntryCount: (int)Math.Min(declaredEntryCount.Value, int.MaxValue),
                LargestEntrySize: 0,
                LargestTextEntrySize: 0);
            throw CreateLimitExceededException(PaArchiveLimit.EntryCount, stats, limits, logger);
        }
    }

    private static void ThrowIfLimitExceeded(ZipArchive zipArchive, PaArchiveLimits limits, ILogger<PaArchive>? logger)
    {
        var stats = ComputeStats(zipArchive, logger);
        var limit = limits.Evaluate(stats);
        if (limit != PaArchiveLimit.None)
            throw CreateLimitExceededException(limit, stats, limits, logger);
    }

    private static PaArchiveLimitExceededException CreateLimitExceededException(PaArchiveLimit limit, PaArchiveStats stats, PaArchiveLimits limits, ILogger<PaArchive>? logger)
    {
        logger?.LogArchiveLimitExceeded(limit, stats.EntryCount, stats.CompressedSize, stats.ExpandedSize, stats.LargestEntrySize, stats.LargestTextEntrySize);
        return new PaArchiveLimitExceededException(limit, stats, limits);
    }
}
