// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
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
        stream.Position = 0;
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

        stream.Position = 0;
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
        using var paArchive = new PaArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        paArchive.ContainsEntry("Assets/img1.jpg").Should().BeTrue();
        paArchive.ContainsEntry("assets/img1.jpg").Should().BeTrue();
        paArchive.ContainsEntry(@"assets\img1.jpg").Should().BeTrue();
        paArchive.Entries.Select(e => e.FullName).Should().Equal(["Header.json", "Assets/img1.jpg".FixupSeparators()], "keys should be normalized even if the entries had upper-case");

        paArchive.TryGetEntry("assets/img1.jpg", out var entry1).Should().BeTrue();
        entry1!.FullName.Should().Be("Assets/img1.jpg".FixupSeparators());
        paArchive.TryGetEntry(@"assets\img1.jpg", out var entry2).Should().BeTrue();
        entry2!.FullName.Should().Be("Assets/img1.jpg".FixupSeparators());
        entry2.Should().BeSameAs(entry1, "both paths should resolve to the same entry instance");
    }

    [TestMethod]
    public void OpenArchiveWithInvalidAndMaliciousEntryPathsTest()
    {
        // Arrange: Create a ZipArchive directly (bypassing PaArchive validation) with a mix of valid
        // entries and entries whose FullName is an invalid or malicious PaArchivePath.
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Valid entries that should appear in PaArchive.Entries
            zipArchive.CreateEntry("Header.json");
            zipArchive.CreateEntry("Assets/img1.jpg");

            // Malicious path traversal entries — should be silently skipped
            zipArchive.CreateEntry("../escape.txt");
            zipArchive.CreateEntry("dir/../../escape.txt");
            zipArchive.CreateEntry(@"c:\System32\drivers\etc\hosts");

            // Entries with invalid path characters — should be silently skipped
            zipArchive.CreateEntry("file|bad.txt");
            zipArchive.CreateEntry("file*bad.txt");
        }

        // Act: Open the archive as PaArchive — must not throw even though some entries have invalid paths
        stream.Position = 0;
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Read);

        // Assert: Only entries with valid paths are exposed via Entries
        paArchive.Entries.Should().HaveCount(2, "only entries with valid paths should be loaded");
        paArchive.ContainsEntry("Header.json").Should().BeTrue();
        paArchive.ContainsEntry("Assets/img1.jpg").Should().BeTrue();
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
