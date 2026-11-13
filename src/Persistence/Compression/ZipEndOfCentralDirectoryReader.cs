// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Buffers.Binary;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Reads the entry count declared in a zip archive's end of central directory record (including the Zip64 record),
/// without reading the central directory itself.
/// </summary>
/// <remarks>
/// Locates the records the same way as <see cref="System.IO.Compression.ZipArchive"/>. When a record is missing or
/// can't be interpreted, no count is returned, so callers can defer to <see cref="System.IO.Compression.ZipArchive"/>'s own handling.
/// </remarks>
internal static class ZipEndOfCentralDirectoryReader
{
    private const uint EndOfCentralDirectorySignature = 0x06054b50;
    private const int EndOfCentralDirectorySize = 22;
    private const int MaxArchiveCommentLength = ushort.MaxValue;

    private const uint Zip64LocatorSignature = 0x07064b50;
    private const int Zip64LocatorSize = 20;

    private const uint Zip64EndOfCentralDirectorySignature = 0x06064b50;
    private const int Zip64EndOfCentralDirectorySize = 56;

    /// <summary>
    /// Reads the declared entry count from a seekable stream positioned anywhere; the stream position is restored afterwards.
    /// </summary>
    /// <returns>The declared entry count, or null when the stream isn't seekable or the records can't be read.</returns>
    public static long? TryReadDeclaredEntryCount(Stream stream)
    {
        ThrowIfNull(stream);

        if (!stream.CanSeek || !stream.CanRead)
            return null;

        var originalPosition = stream.Position;
        try
        {
            return TryReadDeclaredEntryCountCore(stream);
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static long? TryReadDeclaredEntryCountCore(Stream stream)
    {
        var streamLength = stream.Length;
        if (streamLength < EndOfCentralDirectorySize)
            return null;

        // The record is at the end of the archive, and may be followed by a comment of up to 64 KiB.
        var tail = new byte[(int)Math.Min(streamLength, EndOfCentralDirectorySize + MaxArchiveCommentLength)];
        var tailStart = streamLength - tail.Length;
        if (!TryReadAt(stream, tailStart, tail))
            return null;

        // Like ZipArchive, use the signature nearest to the end of the archive
        var eocdIndex = -1;
        for (var i = tail.Length - EndOfCentralDirectorySize; i >= 0; i--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i)) == EndOfCentralDirectorySignature)
            {
                eocdIndex = i;
                break;
            }
        }

        if (eocdIndex < 0)
            return null;

        var eocd = tail.AsSpan(eocdIndex, EndOfCentralDirectorySize);
        var numberOfThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(eocd.Slice(4));
        var entriesOnThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(eocd.Slice(8));
        var entriesTotal = BinaryPrimitives.ReadUInt16LittleEndian(eocd.Slice(10));
        var centralDirectoryOffset = BinaryPrimitives.ReadUInt32LittleEndian(eocd.Slice(16));

        // ZipArchive rejects split archives
        if (entriesTotal != entriesOnThisDisk)
            return null;

        // ZipArchive only looks for the Zip64 records when one of these values is at its maximum
        if (numberOfThisDisk != ushort.MaxValue && entriesTotal != ushort.MaxValue && centralDirectoryOffset != uint.MaxValue)
            return entriesTotal;

        var eocdPosition = tailStart + eocdIndex;
        if (eocdPosition < Zip64LocatorSize)
            return null;

        // The Zip64 locator immediately precedes the end of central directory record. Without it, ZipArchive uses the values read above.
        var locator = new byte[Zip64LocatorSize];
        if (!TryReadAt(stream, eocdPosition - Zip64LocatorSize, locator))
            return null;
        if (BinaryPrimitives.ReadUInt32LittleEndian(locator) != Zip64LocatorSignature)
            return entriesTotal;

        var zip64RecordPosition = BinaryPrimitives.ReadUInt64LittleEndian(locator.AsSpan(8));
        if (zip64RecordPosition > (ulong)streamLength)
            return null;

        var zip64Record = new byte[Zip64EndOfCentralDirectorySize];
        if (!TryReadAt(stream, (long)zip64RecordPosition, zip64Record)
            || BinaryPrimitives.ReadUInt32LittleEndian(zip64Record) != Zip64EndOfCentralDirectorySignature)
        {
            return null;
        }

        var zip64EntriesOnThisDisk = BinaryPrimitives.ReadUInt64LittleEndian(zip64Record.AsSpan(24));
        var zip64EntriesTotal = BinaryPrimitives.ReadUInt64LittleEndian(zip64Record.AsSpan(32));
        if (zip64EntriesTotal > long.MaxValue || zip64EntriesTotal != zip64EntriesOnThisDisk)
            return null;

        return (long)zip64EntriesTotal;
    }

    private static bool TryReadAt(Stream stream, long position, byte[] buffer)
    {
        if (position < 0 || position > stream.Length - buffer.Length)
            return false;

        stream.Position = position;
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (read == 0)
                return false;
            totalRead += read;
        }

        return true;
    }
}
