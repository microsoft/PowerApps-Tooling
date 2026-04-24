// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using static Persistence.Tests.Compression.TestStringExtensions;

namespace Persistence.Tests.Compression;

[TestClass]
public class PaArchiveTests : TestBase
{
    [TestMethod]
    public void GetEntriesInDirectoryTests()
    {
        // Arrange: Create new ZipArchive in memory
        using var stream = new MemoryStream();
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true);
        string[] entries = [
            "0abc.txt",
            "0def.jpg",
            "0foo.pa.yaml",
            "0bar.pa.yaml",
            "dir1/",
            "dir1/1abc.txt",
            "dir1/1def.jpg",
            "dir1/1foo.pa.yaml",
            "dir1/1bar.pa.yaml",
            "dir1/dira/",
            "dir1/dirB/1Babc.txt",
            "dir1/dirB/1Bef.jpg",
            "dir1/dirB/1Boo.pa.yaml",
            "dir1/dirB/1Bar.pa.yaml",
            ];
        foreach (var entry in entries)
        {
            zipArchive.CreateEntry(entry);
        }
        zipArchive.Dispose();

        // Act: Open the archive as MsappArchive
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read);
        paArchive.Entries.Should().HaveCount(12, "only non-directory entries should be available");

        // Assert
        paArchive.GetEntriesInDirectory("", recursive: true).Should().HaveCount(paArchive.Entries.Count, "these parameters should be equivalent to getting all entires");
        paArchive.GetEntriesInDirectory("").Select(e => e.FullName).Should().Equal([
            "0abc.txt",
            "0def.jpg",
            "0foo.pa.yaml",
            "0bar.pa.yaml",
            ], "default overload should be non-recursive");
        paArchive.GetEntriesInDirectory("dir1").Select(e => e.FullName).Should().Equal(FixupSeparators([
            "dir1/1abc.txt",
            "dir1/1def.jpg",
            "dir1/1foo.pa.yaml",
            "dir1/1bar.pa.yaml",
            ]));
        paArchive.GetEntriesInDirectory("dir1", recursive: true).Select(e => e.FullName).Should().Equal(FixupSeparators([
            "dir1/1abc.txt",
            "dir1/1def.jpg",
            "dir1/1foo.pa.yaml",
            "dir1/1bar.pa.yaml",
            "dir1/dirB/1Babc.txt",
            "dir1/dirB/1Bef.jpg",
            "dir1/dirB/1Boo.pa.yaml",
            "dir1/dirB/1Bar.pa.yaml",
            ]));
        paArchive.GetEntriesInDirectory("dir1", recursive: true, extension: ".pa.yaml").Select(e => e.FullName).Should().Equal(FixupSeparators([
            "dir1/1foo.pa.yaml",
            "dir1/1bar.pa.yaml",
            "dir1/dirB/1Boo.pa.yaml",
            "dir1/dirB/1Bar.pa.yaml",
            ]));
        paArchive.GetEntriesInDirectory("dir1", recursive: true, extension: "pg").Select(e => e.FullName).Should().Equal(FixupSeparators([
            "dir1/1def.jpg",
            "dir1/dirB/1Bef.jpg",
            ]), "extension parameter is just a suffix match");
    }

    [TestMethod]
    [DynamicData(nameof(AddEntryTestsData))]
    public void AddEntryTests(string[] entries, string[] expectedEntries)
    {
        // Arrange: Create new ZipArchive in memory
        using var stream = new MemoryStream();
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Create);
        foreach (var entry in entries)
        {
            paArchive.CreateEntry(entry).Should().NotBeNull();
        }

        // Assert
        paArchive.Entries.Should().HaveCount(entries.Length);
        foreach (var expectedEntry in expectedEntries)
        {
            paArchive.ContainsEntry(expectedEntry).Should().BeTrue($"Expected entry {expectedEntry} to exist in the archive");
        }

        // Get the required entry should throw if it doesn't exist
        paArchive.Invoking(a => a.GetRequiredEntry("not-exist")).Should().Throw<PersistenceLibraryException>()
            .WithErrorCode(PersistenceErrorCode.PaArchiveMissingRequiredEntry);
        paArchive.TryGetEntry("not-exist", out var _).Should().BeFalse();
    }

    private static IEnumerable<object[]> AddEntryTestsData()
    {
        yield return new string[][] {
            [ "abc.txt" ],
            [ "abc.txt" ]
        };
        yield return new string[][] {
            [ "abc.txt", @"Resources\abc.txt" ],
            [ "abc.txt", @"Resources/abc.txt".ToLowerInvariant() ],
        };
        yield return new string[][] {
            [ "abc.txt", @"Resources\DEF.txt" ],
            [ "abc.txt", @"Resources/DEF.txt".ToLowerInvariant() ],
        };
        yield return new string[][] {
            [ "abc.txt", @"Resources\DEF.txt", @"\start-with-slash\test.json" ],
            [ "abc.txt", @"Resources/DEF.txt".ToLowerInvariant(), @"start-with-slash/test.json" ],
        };
    }

    /// <summary>
    /// Validates baseline behavior of <see cref="ZipArchive"/> so we can see the difference.
    /// </summary>
    [TestMethod]
    public void ZipArchiveEntryPathTests()
    {
        using var stream = new MemoryStream();
        using (var zipArchiveWrite = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zipArchiveWrite.CreateEntry("dir/file1.txt");
            entry.Name.Should().Be("file1.txt");

            zipArchiveWrite.CreateEntry("/dir/file2.txt");
            zipArchiveWrite.CreateEntry(@"\dir\file3.txt");
            entry = zipArchiveWrite.CreateEntry("dir/");
            entry.Name.Should().Be("");
        }

        using var zipArchiveRead = new ZipArchive(stream, ZipArchiveMode.Read);
        zipArchiveRead.Entries.Select(e => e.FullName).Should().BeEquivalentTo([
            "dir/file1.txt",
            "/dir/file2.txt",
            @"\dir\file3.txt",
            "dir/",
            ], "ZipArchive entry paths are not normalized and assumed to be correct for the current OS");
    }

    [TestMethod]
    public void ContainsEntryTests()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var paArchive = new PaArchive(archiveMemStream, ZipArchiveMode.Create);
        paArchive.CreateEntry("entryA");
        paArchive.CreateEntry("entryB");
        paArchive.CreateEntry("dir1/entryA");
        paArchive.CreateEntry("dir1/entryB");
        paArchive.CreateEntry("dir2/entryA");
        paArchive.CreateEntry("dir2/entryC");

        // Test for entries that should exist, exact case
        paArchive.ContainsEntry("entryA").Should().BeTrue();
        paArchive.ContainsEntry("entryB").Should().BeTrue();
        paArchive.ContainsEntry("dir1/entryA").Should().BeTrue();
        paArchive.ContainsEntry("dir1/entryB").Should().BeTrue();
        paArchive.ContainsEntry("dir2/entryA").Should().BeTrue();
        paArchive.ContainsEntry("dir2/entryC").Should().BeTrue();

        // Should exist, but not exact case or may use non-normalized path
        paArchive.ContainsEntry("ENTRYa").Should().BeTrue();
        paArchive.ContainsEntry("entryB").Should().BeTrue();
        paArchive.ContainsEntry("dir1/entryA").Should().BeTrue();
        paArchive.ContainsEntry("Dir1\\ENTRYa").Should().BeTrue();
        paArchive.ContainsEntry("dir1/entryb").Should().BeTrue();
        paArchive.ContainsEntry("dir1\\entryb").Should().BeTrue();

        // Test for entries that should not exist
        paArchive.ContainsEntry("entryC").Should().BeFalse();
        paArchive.ContainsEntry("entryC").Should().BeFalse();
    }

    [TestMethod]
    public void ContainsEntryWorksWithNewEntriesCreated()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var paArchive = new PaArchive(archiveMemStream, ZipArchiveMode.Create);
        paArchive.CreateEntry("entryA");
        paArchive.CreateEntry("entryB");
        paArchive.CreateEntry("dir2/entryA");
        paArchive.CreateEntry("dir2/entryC");

        // Make sure our new entry does not exist, can be created, and then exists
        paArchive.ContainsEntry("dir1/newEntryD").Should().BeFalse();
        paArchive.CreateEntry("dir1/newEntryD");
        paArchive.ContainsEntry("dir1/newEntryD").Should().BeTrue();
    }

    [TestMethod]
    public void OpenArchiveWithDuplicateEntriesTest()
    {
        using var archiveStream = new MemoryStream();
        SaveNewMinMsappWithHeaderOnly(archiveStream);
        // Manually add duplicate entries to the archive
        using (var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: true))
        {
            zipArchive.Entries.Should().HaveCount(1, "only the Header.json should exist");

            zipArchive.CreateEntry("Assets/img1.jpg");
            zipArchive.CreateEntry(@"assets\img1.jpg");

            zipArchive.Entries.Should().HaveCount(3);
        }

        // Read the archive using MsappArchive
        var capturingLogger = new CapturingLogger<PaArchive>();
        using var paArchive = new PaArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true, logger: capturingLogger);
        paArchive.ContainsEntry("Assets/img1.jpg").Should().BeTrue();
        paArchive.ContainsEntry("assets/img1.jpg").Should().BeTrue();
        paArchive.ContainsEntry(@"assets\img1.jpg").Should().BeTrue();
        paArchive.Entries.Select(e => e.FullName).Should().Equal(["Header.json", "Assets/img1.jpg".FixupSeparators()], "keys should be normalized even if the entries had upper-case");
        paArchive.Entries.Select(e => e.ZipEntry.FullName).Should().Equal(["Header.json", "Assets/img1.jpg"], "verify which entry was kept by looking at ZipEntry.FullName");

        paArchive.TryGetEntry("assets/img1.jpg", out var entry1).Should().BeTrue();
        entry1!.FullName.Should().Be("Assets/img1.jpg".FixupSeparators());
        paArchive.TryGetEntry(@"assets\img1.jpg", out var entry2).Should().BeTrue();
        entry2!.FullName.Should().Be("Assets/img1.jpg".FixupSeparators());
        entry2.Should().BeSameAs(entry1, "both paths should resolve to the same entry instance");

        // The duplicate entry should produce exactly one Warning
        capturingLogger.Entries.Should().ContainSingle(
            e => e.Level == LogLevel.Warning && e.Message.Contains(@"'assets\img1.jpg'"),
            "the second case-insensitive duplicate should be logged as a warning")
            .Which.Message.Should().Match("Duplicate normalized entry found in zip archive*");
        capturingLogger.Entries.Should().HaveCount(1, "only the duplicate entry should produce a log entry");
    }

    [TestMethod]
    public void OpenArchiveWithMaliciousEntryPathsTest()
    {
        // Arrange: Create a ZipArchive directly (bypassing PaArchive validation) with a mix of valid
        // entries and entries whose FullName is an invalid or malicious PaArchivePath.
        // Note: only path-traversal entries are used here because they can be created on all TFMs.
        // For entries with illegal path chars (e.g. '|', '*') see OpenArchiveWithIllegalPathCharEntryTest.
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Valid entries that should appear in PaArchive.Entries
            zipArchive.CreateEntry("Header.json");
            zipArchive.CreateEntry("Assets/img1.jpg");

            // Malicious path traversal entries — should be ignored and logged as warnings
            zipArchive.CreateEntry("../escape.txt");
            zipArchive.CreateEntry("dir/../../escape.txt");
            zipArchive.CreateEntry(@"c:\System32\drivers\etc\hosts");
        }

        // Act: Open the archive as PaArchive — must not throw even though some entries have invalid paths
        var capturingLogger = new CapturingLogger<PaArchive>();
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read, logger: capturingLogger);

        // Assert: Only entries with valid paths are exposed via Entries
        paArchive.Entries.Should().HaveCount(2, "only entries with valid paths should be loaded");
        paArchive.ContainsEntry("Header.json").Should().BeTrue();
        paArchive.ContainsEntry("Assets/img1.jpg").Should().BeTrue();

        // Each invalid/malicious entry should produce exactly one Warning
        capturingLogger.Entries.Should().AllSatisfy(e =>
        {
            e.Level.Should().Be(LogLevel.Warning);
            e.Message.Should().Match("An entry found in zip archive has an invalid or malicious path and will be ignored*");
        });
        capturingLogger.Entries.Should().ContainSingle(e => e.Message.Contains("'../escape.txt'"));
        capturingLogger.Entries.Should().ContainSingle(e => e.Message.Contains("'dir/../../escape.txt'"));
        capturingLogger.Entries.Should().ContainSingle(e => e.Message.Contains(@"'c:\System32\drivers\etc\hosts'"));
        capturingLogger.Entries.Should().HaveCount(3, "one warning per malicious entry path");
    }

    private static IEnumerable<(string FileName, string IllegalEntryName, bool CheckInvalidPathCharsFailsOnNetFx)> IllegalPathCharZipTestDataTuples()
    {
        // The following chars are in Path.GetInvalidPathChars() on .NET Framework 4.8, so Path.CheckInvalidPathChars throws
        yield return ("ContainsIllegalPathChar-pipe.zip", "file|bad.txt", true);

        // The following are invalid chars for PaArchivePath's, but do NOT throw in Path.GetInvalidPathChars
        yield return ("ContainsIllegalPathChar-asterisk.zip", "file*bad.txt", false);
    }

    /// <summary>
    /// Generates the zip test data files used by <see cref="OpenArchiveWithIllegalPathCharEntryTest"/>.
    /// Run this test on .NET (net8.0 or net10.0) when the test data files need to be regenerated,
    /// then copy the output files from the output folder into the source code folder at <c>_TestData/MaliciousZips/</c>.
    ///
    /// Technically, this test doesn't test our product code, but does confirm the expectations we have found around the implementation
    /// differences for ZipArchive for different TFMs.
    /// </summary>
    [TestMethod]
    public void GenerateMaliciousZipTestData()
    {
        if (CurrentTfmIsNetFramework)
        {
            TestContext.WriteLine($"WARNING: DO NOT use the output files from NetFx, as they are expected to not have the required invalid entries");
        }

        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        TestContext.WriteLine($"Saving all zips to folder: {testDir}");

        foreach (var testCase in IllegalPathCharZipTestDataTuples())
        {
            // on net48 CreateEntry calls Path.CheckInvalidPathChars
            var expectCreateEntryToFail = CurrentTfmIsNetFramework && testCase.CheckInvalidPathCharsFailsOnNetFx;
            var zipPath = Path.Combine(testDir, testCase.FileName);
            CreateTestZip(testCase, zipPath, expectCreateEntryToFail);
            CheckTestZip(zipPath, expectCreateEntryToFail);
        }

        void CreateTestZip((string FileName, string IllegalEntryName, bool CheckInvalidPathCharsFailsOnNetFx) testCase, string zipPath, bool expectCreateEntryToFail)
        {
            TestContext.WriteLine($"Saving to: {testCase.FileName}");
            using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Create);

            zipArchive.CreateEntry("validPath.txt");

            if (expectCreateEntryToFail)
            {
                FluentActions.Invoking(() => zipArchive.CreateEntry(testCase.IllegalEntryName))
                    .Should().ThrowExactly<ArgumentException>("CreateEntry on .NET Framework should throw for illegal OS path chars")
                    .WithMessage("*Illegal characters in path*")
                    .Which.StackTrace.Should().Contain("Path.CheckInvalidPathChars");
            }
            else
            {
                zipArchive.CreateEntry(testCase.IllegalEntryName).Should().NotBeNull();
            }
        }

        void CheckTestZip(string zipPath, bool expectCreateEntryToFail)
        {
            using var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read);
            using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);
            TestContext.WriteLine($"Entries in {Path.GetFileName(zipPath)}:");
            foreach (var entry in zipArchive.Entries)
            {
                TestContext.WriteLine($"- {entry.FullName}");
            }

            if (expectCreateEntryToFail)
            {
                zipArchive.Entries.Should().HaveCount(1, "CreateEntry on .NET Framework should should not add entry with invalid path chars");
            }
            else
            {
                zipArchive.Entries.Should().HaveCount(2);
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(IllegalPathCharZipTestDataTuples))]
    public void OpenArchiveWithIllegalPathCharEntryTest(string zipFileName, string illegalEntryName, bool checkInvalidPathCharsFailsOnNetFx)
    {
        // Entries with Windows-illegal path chars in a pre-existing zip are handled gracefully on all
        // TFMs: PaArchive skips the entry and logs a warning. On .NET Framework 4.8, chars that are in
        // Path.GetInvalidPathChars() (e.g. '|') cause ZipArchive.Entries to throw, effectively blocking
        // the use of that zip archive on net48.
        var zipPath = Path.Combine("_TestData", "MaliciousZips", zipFileName);
        var capturingLogger = new CapturingLogger<PaArchive>();
        using var stream = File.OpenRead(zipPath);
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read, logger: capturingLogger);

        if (CurrentTfmIsNetFramework && checkInvalidPathCharsFailsOnNetFx)
        {
            // When ZipArchive.Entries throws due to invalid path chars on NetFramework, we expect PaArchive to also throw.
            // This is a limitation of the ZipArchive implementation on that platform.
            // Essentially, it's all-or-nothing on NetFramework, but best made progress on .NET.
            var exThrown = FluentActions.Invoking(() => { _ = paArchive.Entries; })
                .Should().Throw<PersistenceLibraryException>()
                .WithErrorCode(PersistenceErrorCode.ZipArchiveBlockedDueToInvalidEntryPathCharsOnNetFramework)
                .Which;
            exThrown.StackTrace.Should().Contain("PaArchive.EnsureEntriesInitialized");
            exThrown.InnerException.Should().BeOfType<ArgumentException>()
                .Which.StackTrace.Should().Contain("Path.CheckInvalidPathChars");
            capturingLogger.EntriesByName(nameof(PAPersistenceLog.LogZipArchiveBlockedDueToInvalidEntryPathCharsOnNetFramework)).Should().ContainSingle();
            capturingLogger.EntriesByName(nameof(PAPersistenceLog.LogInvalidPathEntryIgnored)).Should().BeEmpty();
        }
        else
        {
            // Otherwise, we should be able to use the valid entries
            paArchive.Should().HaveEntry("validPath.txt")
                .And.NotHaveEntry(illegalEntryName);
            paArchive.Entries.Should().HaveCount(1, "only the valid entry should be loaded; the illegal-path entry should be skipped");
            capturingLogger.EntriesByName(nameof(PAPersistenceLog.LogInvalidPathEntryIgnored)).Should().ContainSingle()
                .Which.Message.Should().Contain($"InvalidReason: 'InvalidPathChars';")
                .And.Contain($"EntryFullName: '{illegalEntryName}'");
        }
    }

    [TestMethod]
    public void DirectoryEntriesIgnoredTest()
    {
        // Arrange: Create a ZipArchive directly with a mix of valid file entries and directory entries,
        // including one directory entry that unexpectedly contains data (non-zero length).
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            zipArchive.CreateEntry("Header.json");
            zipArchive.CreateEntry("Assets/img1.jpg");

            // A normal empty directory entry — should be ignored (logged at Information level)
            zipArchive.CreateEntry("Assets/");
        }

        var capturingLogger = new CapturingLogger<PaArchive>();
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read, logger: capturingLogger);

        // Assert: directory entries are ignored regardless of whether they have data
        paArchive.Should().HaveEntry("Header.json");
        paArchive.Should().HaveEntry("Assets/img1.jpg");
        paArchive.Entries.Should().HaveCount(2, "only file entries should be loaded");

        // directory entries should produce a Warning that they are ignored
        capturingLogger.Entries.Should().ContainSingle(
            e => e.Level == LogLevel.Information && e.Message.Contains("'Assets/'"),
            "all directory entries should produce a Warning when ignored")
            .Which.Message.Should().Match("Directory entries found in zip archives are ignored.*");

        // The directory entry with data should additionally produce a second Warning about the unexpected data
        capturingLogger.Entries.Should().HaveCount(1, "1 ignored-directory warnings");
    }

    [TestMethod]
    public void DirectoryEntryWithDataProducesWarningButStillIgnoredTest()
    {
        // Arrange: Create a ZipArchive directly with a mix of valid file entries and directory entries,
        // including one directory entry that unexpectedly contains data (non-zero length).
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            zipArchive.CreateEntry("Header.json");

            // A directory entry that has data — should still be ignored, but logged at Warning level
            var dirEntryWithData = zipArchive.CreateEntry("DirEntryWithData/");
            using var writer = new StreamWriter(dirEntryWithData.Open());
            writer.Write("unexpected data in a directory entry");
        }

        var capturingLogger = new CapturingLogger<PaArchive>();
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read, logger: capturingLogger);

        // Assert: directory entries are ignored regardless of whether they have data
        paArchive.Should().HaveEntry("Header.json");
        paArchive.Entries.Should().HaveCount(1, "only file entries should be loaded");

        // directory entries should produce a Warning that they are ignored
        capturingLogger.Entries.Should().ContainSingle(
            e => e.Level == LogLevel.Information && e.Message.Contains("'DirEntryWithData/'"),
            "all directory entries should produce a Warning when ignored")
            .Which.Message.Should().Match("Directory entries found in zip archives are ignored.*");

        // And an additional warning should be logged for the dir entry that has data
        capturingLogger.Entries.Should().ContainSingle(
            e => e.Level == LogLevel.Warning && e.Message.Contains("'DirEntryWithData/'"),
            "all directory entries should produce a Warning when ignored")
            .Which.Message.Should().Match("A directory entry with non-zero data length was found in zip archive.*");

        capturingLogger.Entries.Should().HaveCount(2, "1 ignored-directory warnings + 1 extra warning for data in DirEntryWithData/");
    }

    [TestMethod]
    public void DeleteEntryTests()
    {
        // Arrange: Create archive with several entries
        using var stream = new MemoryStream();
        using (var createArchive = new PaArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            createArchive.CreateEntry("file1.txt");
            createArchive.CreateEntry("file2.txt");
            createArchive.CreateEntry("dir1/file3.txt");
        }

        // Act: Open in Update mode and delete one entry
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Update);
        paArchive.Entries.Should().HaveCount(3);

        var entry = paArchive.GetRequiredEntry("file1.txt");
        entry.Delete();

        // Assert: Deleted entry no longer visible; others remain
        paArchive.ContainsEntry("file1.txt").Should().BeFalse();
        paArchive.ContainsEntry("file2.txt").Should().BeTrue();
        paArchive.ContainsEntry("dir1/file3.txt").Should().BeTrue();
        paArchive.Entries.Should().HaveCount(2);
    }

    [TestMethod]
    public void DeleteAllEntriesTest()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var createArchive = new PaArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            createArchive.CreateEntry("file1.txt");
            createArchive.CreateEntry("file2.txt");
        }

        // Act: Delete every entry
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Update);
        foreach (var entry in paArchive.Entries.ToList())
            entry.Delete();

        // Assert
        paArchive.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public void ReadAllLinesTests()
    {
        // Arrange: Write a multi-line entry (no trailing newline on last line)
        using var stream = new MemoryStream();
        using (var createArchive = new PaArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = createArchive.CreateEntry("file.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.WriteLine("line1");
            writer.WriteLine("line2");
            writer.Write("line3");
        }

        // Act
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read);
        var lines = paArchive.GetRequiredEntry("file.txt").ReadAllLines();

        // Assert
        lines.Should().Equal(["line1", "line2", "line3"]);
    }

    [TestMethod]
    public void ReadAllLinesEmptyEntryTest()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var createArchive = new PaArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            createArchive.CreateEntry("empty.txt");

        // Act
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read);
        var lines = paArchive.GetRequiredEntry("empty.txt").ReadAllLines();

        // Assert
        lines.Should().BeEmpty();
    }

    [TestMethod]
    public void ReadAllTextTests()
    {
        // Arrange
        const string content = "Hello, World!\nThis is a test.";
        using var stream = new MemoryStream();
        using (var createArchive = new PaArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = createArchive.CreateEntry("file.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        // Act
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read);
        var text = paArchive.GetRequiredEntry("file.txt").ReadAllText();

        // Assert
        text.Should().Be(content);
    }

    [TestMethod]
    public void ReadAllTextEmptyEntryTest()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var createArchive = new PaArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            createArchive.CreateEntry("empty.txt");

        // Act
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read);
        var text = paArchive.GetRequiredEntry("empty.txt").ReadAllText();

        // Assert
        text.Should().BeEmpty();
    }

    private static void SaveNewMinMsappWithHeaderOnly(MemoryStream archiveStream, string headerFileName = "Header-DocV-1.347.json")
    {
        // Create an msapp-like archive with minimum required content. For this test, it's just the header.json file.
        using var writeToArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true);
        var headerFilePath = Path.Combine("_TestData", "headers", headerFileName);
        writeToArchive.CreateEntryFromFile(headerFilePath, "Header.json");
    }
}

internal static class TestStringExtensions
{
    /// <summary>
    /// Normalizes an assumed path by replacing non-platform separator chars with the platform separator.
    /// This allows test code to be written with consistent separators regardless of the OS it's running on, while still validating that the underlying ZipArchive entries are created with the expected separators.
    /// </summary>
    public static string FixupSeparators(this string path)
    {
        if (Path.DirectorySeparatorChar == '/')
            return path.Replace('\\', '/');
        else
            return path.Replace('/', '\\');
    }

    public static IEnumerable<string> FixupSeparators(IEnumerable<string> paths) => paths.Select(FixupSeparators);
}
