// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

namespace Persistence.Tests.Compression;

[TestClass]
public class PaArchiveLimitsTests : TestBase
{
    /// <summary>
    /// Limits that none of the small archives in these tests come close to.
    /// Tests lower a single value via a 'with' expression to target one limit.
    /// </summary>
    private static readonly PaArchiveLimits NonRestrictiveLimits = new()
    {
        MaxExpandedSize = 1_000_000,
        MaxCompressedSize = 1_000_000,
        MaxCompressionRatio = 1_000_000,
        RatioCheckMinExpandedSize = 1_000_000,
        MaxEntrySize = 1_000_000,
        MaxEntryCount = 1_000,
        MaxTextEntrySize = 1_000_000,
    };

    private static readonly PaArchiveLimits EvaluateLimits = new()
    {
        MaxEntryCount = 10,
        MaxCompressedSize = 1_000,
        MaxExpandedSize = 10_000,
        MaxEntrySize = 500,
        MaxTextEntrySize = 200,
        RatioCheckMinExpandedSize = 1_000,
        MaxCompressionRatio = 5,
    };

    #region Evaluate

    [TestMethod]
    public void EvaluateReturnsNoneWhenValuesEqualLimits()
    {
        // ratio = 5_000 / 1_000 = 5, which equals MaxCompressionRatio
        var stats = new PaArchiveStats(CompressedSize: 1_000, ExpandedSize: 5_000, EntryCount: 10, LargestEntrySize: 500, LargestTextEntrySize: 200);

        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.None);
    }

    [TestMethod]
    public void EvaluateReturnsFirstExceededLimitInOrder()
    {
        // Start with every limit exceeded, then bring them within limits one at a time.
        var stats = new PaArchiveStats(CompressedSize: 1_001, ExpandedSize: 10_001, EntryCount: 11, LargestEntrySize: 501, LargestTextEntrySize: 201);
        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.EntryCount);

        stats = stats with { EntryCount = 10 };
        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.CompressedSize);

        stats = stats with { CompressedSize = 1_000 };
        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.ExpandedSize);

        stats = stats with { ExpandedSize = 10_000 };
        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.EntrySize);

        stats = stats with { LargestEntrySize = 500 };
        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.TextEntrySize);

        stats = stats with { LargestTextEntrySize = 200 };
        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.CompressionRatio, "ratio is 10_000 / 1_000 = 10");

        stats = stats with { ExpandedSize = 5_000 };
        EvaluateLimits.Evaluate(stats).Should().Be(PaArchiveLimit.None);
    }

    [TestMethod]
    [DataRow(1_000L, 1L, PaArchiveLimit.None)] // ratio not checked at the threshold
    [DataRow(1_000L, 0L, PaArchiveLimit.None)]
    [DataRow(1_001L, 201L, PaArchiveLimit.None)] // ratio 4.98
    [DataRow(1_001L, 200L, PaArchiveLimit.CompressionRatio)] // ratio 5.005
    [DataRow(1_001L, 1L, PaArchiveLimit.CompressionRatio)]
    [DataRow(1_001L, 0L, PaArchiveLimit.CompressionRatio)]
    [DataRow(1_001L, -1L, PaArchiveLimit.CompressionRatio)]
    public void EvaluateChecksCompressionRatioOnlyAboveThreshold(long expandedSize, long compressedSize, PaArchiveLimit expected)
    {
        var stats = new PaArchiveStats(compressedSize, expandedSize, EntryCount: 1, LargestEntrySize: 0, LargestTextEntrySize: 0);

        EvaluateLimits.Evaluate(stats).Should().Be(expected);
    }

    #endregion

    #region Stats

    [TestMethod]
    public void FromEntriesComputesTotalsAndLargestSizes()
    {
        var stats = PaArchiveStats.FromEntries([
            ("Header.json", 10, 5),
            ("Src/App.pa.yaml", 30, 8),
            ("Assets/Images/logo.png", 100, 90),
            ("References/Notes.TXT", 20, 4),
            ("other.Yml", 25, 4),
            ("data.XML", 15, 3),
            ("folder.json/", 0, 0),
            ("notjson", 50, 1),
        ]);

        stats.Should().Be(new PaArchiveStats(CompressedSize: 115, ExpandedSize: 250, EntryCount: 8, LargestEntrySize: 100, LargestTextEntrySize: 30));
    }

    [TestMethod]
    public void FromEntriesSaturatesInsteadOfOverflowing()
    {
        var stats = PaArchiveStats.FromEntries([
            ("a.bin", long.MaxValue - 1, long.MaxValue - 1),
            ("b.bin", 2, 2),
            ("c.json", -1, -1),
        ]);

        stats.ExpandedSize.Should().Be(long.MaxValue);
        stats.CompressedSize.Should().Be(long.MaxValue);
        stats.LargestEntrySize.Should().Be(long.MaxValue);
        stats.LargestTextEntrySize.Should().Be(long.MaxValue, "negative sizes are treated as the largest possible size");
        stats.EntryCount.Should().Be(3);
    }

    [TestMethod]
    public void GetStatsIncludesEntriesIgnoredByPaArchiveWithoutLoadingEntries()
    {
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zipArchive, "Header.json", new string('h', 10));
            AddEntry(zipArchive, "Assets/img1.jpg", new string('a', 20));
            AddEntry(zipArchive, @"assets\IMG1.jpg", new string('b', 30)); // duplicate after normalization
            AddEntry(zipArchive, "Assets/", "");
            AddEntry(zipArchive, "DirWithData/", new string('d', 40));
            AddEntry(zipArchive, "../outside.txt", new string('o', 50)); // invalid path
        }
        var expectedStats = GetStatsUsingZipArchive(stream.ToArray());

        var capturingLogger = new CapturingLogger<PaArchive>();
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read, logger: capturingLogger);

        var stats = paArchive.GetStats();

        capturingLogger.Entries.Should().BeEmpty("computing stats should not load the normalized entries");
        stats.Should().Be(expectedStats);
        stats.EntryCount.Should().Be(6);
        stats.ExpandedSize.Should().Be(150);
        stats.LargestEntrySize.Should().Be(50);
        stats.LargestTextEntrySize.Should().Be(50, "'../outside.txt' counts as a text entry even though PaArchive ignores it");

        paArchive.Entries.Should().HaveCount(2);
        capturingLogger.Entries.Should().NotBeEmpty("loading entries logs the ignored ones");
    }

    [TestMethod]
    public void GetStatsThrowsInCreateMode()
    {
        using var stream = new MemoryStream();
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Create);

        paArchive.Invoking(a => a.GetStats()).Should().Throw<NotSupportedException>();
    }

    #endregion

    #region Opening with limits

    [TestMethod]
    public void OpenWithoutLimitsDoesNotReadEntriesUpFront()
    {
        // The declared entry count doesn't match the central directory, which ZipArchive only notices when reading entries.
        var archiveBytes = CreateSampleArchive();
        SetDeclaredEntryCount(archiveBytes, 1_000);

        using var paArchive = new PaArchive(ToStream(archiveBytes), ZipArchiveMode.Read);

        paArchive.Limits.Should().BeNull();
        paArchive.Invoking(a => a.Entries).Should().Throw<InvalidDataException>();
    }

    [TestMethod]
    [DataRow(ZipArchiveMode.Read)]
    [DataRow(ZipArchiveMode.Update)]
    public void OpenWithinLimitsSucceeds(ZipArchiveMode mode)
    {
        var archiveBytes = CreateSampleArchive();
        using var stream = ToStream(archiveBytes);

        using (var paArchive = new PaArchive(stream, mode, NonRestrictiveLimits, leaveOpen: true))
        {
            paArchive.Limits.Should().BeSameAs(NonRestrictiveLimits);
            paArchive.Entries.Should().HaveCount(2);
            paArchive.GetRequiredEntry("Header.json").ReadAllText().Should().Be(new string('h', 100));
            if (mode == ZipArchiveMode.Update)
            {
                paArchive.CreateEntry("Added.txt");
            }
        }

        stream.Position = 0;
        using var reread = new PaArchive(stream, ZipArchiveMode.Read);
        reread.Entries.Should().HaveCount(mode == ZipArchiveMode.Update ? 3 : 2);
    }

    [TestMethod]
    [DataRow(PaArchiveLimit.CompressedSize, ZipArchiveMode.Read)]
    [DataRow(PaArchiveLimit.CompressedSize, ZipArchiveMode.Update)]
    [DataRow(PaArchiveLimit.ExpandedSize, ZipArchiveMode.Read)]
    [DataRow(PaArchiveLimit.ExpandedSize, ZipArchiveMode.Update)]
    [DataRow(PaArchiveLimit.EntrySize, ZipArchiveMode.Read)]
    [DataRow(PaArchiveLimit.EntrySize, ZipArchiveMode.Update)]
    [DataRow(PaArchiveLimit.TextEntrySize, ZipArchiveMode.Read)]
    [DataRow(PaArchiveLimit.TextEntrySize, ZipArchiveMode.Update)]
    [DataRow(PaArchiveLimit.CompressionRatio, ZipArchiveMode.Read)]
    [DataRow(PaArchiveLimit.CompressionRatio, ZipArchiveMode.Update)]
    public void OpenThrowsWhenLimitExceeded(PaArchiveLimit limit, ZipArchiveMode mode)
    {
        // Sample archive: 2 entries; 400 bytes expanded; largest entry 300 bytes; largest text entry 100 bytes.
        var archiveBytes = CreateSampleArchive();
        var expectedStats = GetStatsUsingZipArchive(archiveBytes);
        var limits = limit switch
        {
            PaArchiveLimit.CompressedSize => NonRestrictiveLimits with { MaxCompressedSize = expectedStats.CompressedSize - 1 },
            PaArchiveLimit.ExpandedSize => NonRestrictiveLimits with { MaxExpandedSize = 399 },
            PaArchiveLimit.EntrySize => NonRestrictiveLimits with { MaxEntrySize = 299 },
            PaArchiveLimit.TextEntrySize => NonRestrictiveLimits with { MaxTextEntrySize = 99 },
            PaArchiveLimit.CompressionRatio => NonRestrictiveLimits with { RatioCheckMinExpandedSize = 399, MaxCompressionRatio = 2 },
            _ => throw new ArgumentOutOfRangeException(nameof(limit)),
        };
        using var stream = ToStream(archiveBytes);

        var ex = FluentActions.Invoking(() => new PaArchive(stream, mode, limits, leaveOpen: true))
            .Should().ThrowExactly<PaArchiveLimitExceededException>()
            .WithErrorCode(PersistenceErrorCode.PaArchiveLimitExceeded)
            .Which;

        ex.Limit.Should().Be(limit);
        ex.Stats.Should().Be(expectedStats);
        ex.Limits.Should().BeSameAs(limits);
        ex.Message.Should().StartWith("[5005:PaArchiveLimitExceeded] ").And.Contain($"'{limit}'");
        stream.ToArray().Should().Equal(archiveBytes, "a rejected open should not modify the archive");
    }

    [TestMethod]
    [DataRow(ZipArchiveMode.Read)]
    [DataRow(ZipArchiveMode.Update)]
    public void OpenThrowsWhenDeclaredEntryCountExceedsLimitBeforeReadingEntries(ZipArchiveMode mode)
    {
        // The declared count doesn't match the central directory, so this can only be rejected
        // with EntryCount if it's checked before ZipArchive reads the entries.
        var archiveBytes = CreateSampleArchive();
        SetDeclaredEntryCount(archiveBytes, 1_000);
        var limits = NonRestrictiveLimits with { MaxEntryCount = 999 };
        using var stream = ToStream(archiveBytes);

        var ex = FluentActions.Invoking(() => new PaArchive(stream, mode, limits, leaveOpen: true))
            .Should().ThrowExactly<PaArchiveLimitExceededException>()
            .Which;

        ex.Limit.Should().Be(PaArchiveLimit.EntryCount);
        ex.Stats.Should().Be(new PaArchiveStats(CompressedSize: 0, ExpandedSize: 0, EntryCount: 1_000, LargestEntrySize: 0, LargestTextEntrySize: 0));
        stream.ToArray().Should().Equal(archiveBytes);
    }

    [TestMethod]
    public void OpenFindsDeclaredEntryCountWhenArchiveHasComment()
    {
        var archiveBytes = CreateSampleArchive();
        SetDeclaredEntryCount(archiveBytes, 1_000);
        archiveBytes = AppendArchiveComment(archiveBytes, "An archive comment.");
        var limits = NonRestrictiveLimits with { MaxEntryCount = 999 };

        FluentActions.Invoking(() => new PaArchive(ToStream(archiveBytes), ZipArchiveMode.Read, limits))
            .Should().ThrowExactly<PaArchiveLimitExceededException>()
            .Which.Stats.EntryCount.Should().Be(1_000);
    }

    [TestMethod]
    [DataRow(70_000L)]
    [DataRow(5_000_000_000L)]
    public void OpenThrowsWhenZip64DeclaredEntryCountExceedsLimit(long declaredEntryCount)
    {
        var archiveBytes = ConvertToZip64EndOfCentralDirectory(CreateSampleArchive(), declaredEntryCount);
        var limits = NonRestrictiveLimits with { MaxEntryCount = 10 };

        var ex = FluentActions.Invoking(() => new PaArchive(ToStream(archiveBytes), ZipArchiveMode.Read, limits))
            .Should().ThrowExactly<PaArchiveLimitExceededException>()
            .Which;

        ex.Limit.Should().Be(PaArchiveLimit.EntryCount);
        ex.Stats.EntryCount.Should().Be((int)Math.Min(declaredEntryCount, int.MaxValue));
    }

    [TestMethod]
    public void OpenWithinLimitsSucceedsForZip64EndOfCentralDirectory()
    {
        var archiveBytes = ConvertToZip64EndOfCentralDirectory(CreateSampleArchive(), declaredEntryCount: 2);
        var limits = NonRestrictiveLimits with { MaxEntryCount = 2 };

        using var paArchive = new PaArchive(ToStream(archiveBytes), ZipArchiveMode.Read, limits);

        paArchive.Entries.Should().HaveCount(2);
    }

    [TestMethod]
    public void OpenChecksEntryCountAfterReadingWhenStreamIsNotSeekable()
    {
        var archiveBytes = CreateSampleArchive();
        var expectedStats = GetStatsUsingZipArchive(archiveBytes);
        var limits = NonRestrictiveLimits with { MaxEntryCount = 1 };

        var ex = FluentActions.Invoking(() => new PaArchive(new NonSeekableReadStream(ToStream(archiveBytes)), ZipArchiveMode.Read, limits))
            .Should().ThrowExactly<PaArchiveLimitExceededException>()
            .Which;

        ex.Limit.Should().Be(PaArchiveLimit.EntryCount);
        ex.Stats.Should().Be(expectedStats);
    }

    [TestMethod]
    [DataRow("NotAZip", ZipArchiveMode.Read)]
    [DataRow("NotAZip", ZipArchiveMode.Update)]
    [DataRow("TooShort", ZipArchiveMode.Read)]
    [DataRow("TooShort", ZipArchiveMode.Update)]
    [DataRow("Empty", ZipArchiveMode.Read)]
    [DataRow("Empty", ZipArchiveMode.Update)]
    [DataRow("CentralDirectoryOffsetOutOfRange", ZipArchiveMode.Read)]
    [DataRow("CentralDirectoryOffsetOutOfRange", ZipArchiveMode.Update)]
    [DataRow("Zip64LocatorOffsetOutOfRange", ZipArchiveMode.Read)]
    [DataRow("Zip64LocatorOffsetOutOfRange", ZipArchiveMode.Update)]
    public void OpenWithLimitsKeepsZipArchiveBehaviorForMalformedArchives(string caseName, ZipArchiveMode mode)
    {
        var archiveBytes = caseName switch
        {
            "NotAZip" => Encoding.UTF8.GetBytes("This content is not a zip archive, but is longer than the minimum size."),
            "TooShort" => [0x50, 0x4b, 0x05, 0x06, 0x00],
            "Empty" => [],
            "CentralDirectoryOffsetOutOfRange" => WithCentralDirectoryOffset(CreateSampleArchive(), 0x7FFF_0000),
            "Zip64LocatorOffsetOutOfRange" => WithZip64LocatorOffset(ConvertToZip64EndOfCentralDirectory(CreateSampleArchive(), 2), 0x7FFF_0000),
            _ => throw new ArgumentOutOfRangeException(nameof(caseName)),
        };

        var exWithoutLimits = CaptureOpenException(archiveBytes, mode, limits: null);
        var exWithLimits = CaptureOpenException(archiveBytes, mode, NonRestrictiveLimits);

        TestContext.WriteLine($"Without limits: {exWithoutLimits?.GetType().Name ?? "<none>"}; With limits: {exWithLimits?.GetType().Name ?? "<none>"}");
        exWithLimits?.GetType().Should().Be(exWithoutLimits?.GetType());
        (exWithLimits is null).Should().Be(exWithoutLimits is null);

        static Exception? CaptureOpenException(byte[] archiveBytes, ZipArchiveMode mode, PaArchiveLimits? limits)
        {
            try
            {
                using var paArchive = new PaArchive(ToStream(archiveBytes), mode, limits);
                _ = paArchive.Entries;
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }

    [TestMethod]
    [DataRow(ZipArchiveMode.Read, true)]
    [DataRow(ZipArchiveMode.Read, false)]
    [DataRow(ZipArchiveMode.Update, true)]
    [DataRow(ZipArchiveMode.Update, false)]
    public void RejectedOpenRespectsLeaveOpen(ZipArchiveMode mode, bool leaveOpen)
    {
        // Covers rejection both before reading entries (EntryCount) and after (EntrySize).
        foreach (var limits in new[] { NonRestrictiveLimits with { MaxEntryCount = 1 }, NonRestrictiveLimits with { MaxEntrySize = 1 } })
        {
            var stream = ToStream(CreateSampleArchive());

            FluentActions.Invoking(() => new PaArchive(stream, mode, limits, leaveOpen))
                .Should().ThrowExactly<PaArchiveLimitExceededException>();

            stream.CanRead.Should().Be(leaveOpen, "the stream should only be disposed when leaveOpen is false");
            stream.Dispose();
        }
    }

    [TestMethod]
    public void CreateModeIsNotChecked()
    {
        var limits = NonRestrictiveLimits with { MaxEntryCount = 0, MaxEntrySize = 0, MaxExpandedSize = 0 };
        using var stream = new MemoryStream();

        using (var paArchive = new PaArchive(stream, ZipArchiveMode.Create, limits, leaveOpen: true))
        {
            paArchive.Limits.Should().BeSameAs(limits);
            using var writer = new StreamWriter(paArchive.CreateEntry("file.txt").Open());
            writer.Write("content");
        }

        stream.Position = 0;
        FluentActions.Invoking(() => new PaArchive(stream, ZipArchiveMode.Read, limits, leaveOpen: true))
            .Should().ThrowExactly<PaArchiveLimitExceededException>("the same limits apply when the archive is opened for reading");
    }

    [TestMethod]
    public void UpdateModeOnEmptyStreamIsNotChecked()
    {
        var limits = NonRestrictiveLimits with { MaxEntryCount = 0 };
        using var stream = new MemoryStream();

        using var paArchive = new PaArchive(stream, ZipArchiveMode.Update, limits);

        paArchive.CreateEntry("file.txt").Should().NotBeNull();
    }

    [TestMethod]
    public void RejectedOpenLogsWarning()
    {
        var capturingLogger = new CapturingLogger<PaArchive>();
        var limits = NonRestrictiveLimits with { MaxEntrySize = 1 };

        FluentActions.Invoking(() => new PaArchive(ToStream(CreateSampleArchive()), ZipArchiveMode.Read, limits, logger: capturingLogger))
            .Should().ThrowExactly<PaArchiveLimitExceededException>();

        capturingLogger.EntriesByName(nameof(PAPersistenceLog.LogArchiveLimitExceeded)).Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Warning);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 2 entries; 400 bytes expanded; largest entry is 300 bytes; largest text entry is 100 bytes; content is highly compressible.
    /// </summary>
    private static byte[] CreateSampleArchive()
    {
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zipArchive, "Header.json", new string('h', 100));
            AddEntry(zipArchive, "Assets/logo.png", new string('p', 300));
        }

        return stream.ToArray();
    }

    private static void AddEntry(ZipArchive zipArchive, string entryName, string content)
    {
        using var entryStream = zipArchive.CreateEntry(entryName).Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        entryStream.Write(bytes, 0, bytes.Length);
    }

    private static PaArchiveStats GetStatsUsingZipArchive(byte[] archiveBytes)
    {
        using var zipArchive = new ZipArchive(new MemoryStream(archiveBytes), ZipArchiveMode.Read);
        return PaArchiveStats.FromEntries(zipArchive.Entries.Select(e => (e.FullName, e.Length, e.CompressedLength)).ToList());
    }

    /// <summary>
    /// Returns an expandable stream, positioned at the start, containing a copy of the bytes.
    /// </summary>
    private static MemoryStream ToStream(byte[] bytes)
    {
        var stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;
        return stream;
    }

    private const uint EndOfCentralDirectorySignature = 0x06054b50;
    private const int EndOfCentralDirectorySize = 22;

    private static int GetEndOfCentralDirectoryOffset(byte[] archiveBytes)
    {
        var offset = archiveBytes.Length - EndOfCentralDirectorySize;
        BitConverter.ToUInt32(archiveBytes, offset).Should().Be(EndOfCentralDirectorySignature, "test archives are expected to not have a comment");
        return offset;
    }

    private static void SetDeclaredEntryCount(byte[] archiveBytes, ushort entryCount)
    {
        var eocdOffset = GetEndOfCentralDirectoryOffset(archiveBytes);
        WriteUInt16(archiveBytes, eocdOffset + 8, entryCount);
        WriteUInt16(archiveBytes, eocdOffset + 10, entryCount);
    }

    private static byte[] WithCentralDirectoryOffset(byte[] archiveBytes, uint centralDirectoryOffset)
    {
        var eocdOffset = GetEndOfCentralDirectoryOffset(archiveBytes);
        WriteUInt32(archiveBytes, eocdOffset + 16, centralDirectoryOffset);
        return archiveBytes;
    }

    private static byte[] AppendArchiveComment(byte[] archiveBytes, string comment)
    {
        var eocdOffset = GetEndOfCentralDirectoryOffset(archiveBytes);
        var commentBytes = Encoding.ASCII.GetBytes(comment);
        WriteUInt16(archiveBytes, eocdOffset + 20, (ushort)commentBytes.Length);
        return [.. archiveBytes, .. commentBytes];
    }

    /// <summary>
    /// Replaces the end of central directory record with a Zip64 end of central directory record, a Zip64 locator, and
    /// an end of central directory record whose entry counts defer to the Zip64 record.
    /// </summary>
    private static byte[] ConvertToZip64EndOfCentralDirectory(byte[] archiveBytes, long declaredEntryCount)
    {
        var eocdOffset = GetEndOfCentralDirectoryOffset(archiveBytes);
        var centralDirectorySize = BitConverter.ToUInt32(archiveBytes, eocdOffset + 12);
        var centralDirectoryOffset = BitConverter.ToUInt32(archiveBytes, eocdOffset + 16);

        using var stream = new MemoryStream();
        stream.Write(archiveBytes, 0, eocdOffset);
        using var writer = new BinaryWriter(stream);

        // Zip64 end of central directory record
        var zip64RecordOffset = stream.Position;
        writer.Write(0x06064b50u);
        writer.Write(44UL); // size of the remaining record
        writer.Write((ushort)45); // version made by
        writer.Write((ushort)45); // version needed to extract
        writer.Write(0u); // number of this disk
        writer.Write(0u); // disk where central directory starts
        writer.Write((ulong)declaredEntryCount); // entries on this disk
        writer.Write((ulong)declaredEntryCount); // total entries
        writer.Write((ulong)centralDirectorySize);
        writer.Write((ulong)centralDirectoryOffset);

        // Zip64 end of central directory locator
        writer.Write(0x07064b50u);
        writer.Write(0u); // disk with the Zip64 end of central directory record
        writer.Write((ulong)zip64RecordOffset);
        writer.Write(1u); // total number of disks

        // End of central directory record
        writer.Write(EndOfCentralDirectorySignature);
        writer.Write((ushort)0); // number of this disk
        writer.Write((ushort)0); // disk where central directory starts
        writer.Write((ushort)0xFFFF); // entries on this disk
        writer.Write((ushort)0xFFFF); // total entries
        writer.Write(centralDirectorySize);
        writer.Write(centralDirectoryOffset);
        writer.Write((ushort)0); // comment length
        writer.Flush();

        return stream.ToArray();
    }

    private static byte[] WithZip64LocatorOffset(byte[] archiveBytes, ulong zip64RecordOffset)
    {
        const int Zip64LocatorSize = 20;
        var locatorOffset = GetEndOfCentralDirectoryOffset(archiveBytes) - Zip64LocatorSize;
        BitConverter.ToUInt32(archiveBytes, locatorOffset).Should().Be(0x07064b50u);
        var offsetBytes = BitConverter.GetBytes(zip64RecordOffset);
        Array.Copy(offsetBytes, 0, archiveBytes, locatorOffset + 8, offsetBytes.Length);
        return archiveBytes;
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        WriteUInt16(bytes, offset, (ushort)value);
        WriteUInt16(bytes, offset + 2, (ushort)(value >> 16));
    }

    private sealed class NonSeekableReadStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                inner.Dispose();
            base.Dispose(disposing);
        }
    }

    #endregion
}
