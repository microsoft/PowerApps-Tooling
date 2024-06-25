// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using Moq;

namespace Persistence.Tests.MsApp;

[TestClass]
public class MsappArchiveTests : TestBase
{
    private readonly Mock<IYamlSerializationFactory> _mockYamlSerializationFactory;

    public MsappArchiveTests()
    {
        _mockYamlSerializationFactory = new(MockBehavior.Strict);
        _mockYamlSerializationFactory.Setup(f => f.CreateSerializer(It.IsAny<YamlSerializationOptions>()))
            .Returns(new Mock<IYamlSerializer>(MockBehavior.Strict).Object);
        _mockYamlSerializationFactory.Setup(f => f.CreateDeserializer(It.IsAny<YamlSerializationOptions>()))
            .Returns(new Mock<IYamlDeserializer>(MockBehavior.Strict).Object);
    }

    [DataRow(new string[] { "abc.txt" }, MsappArchive.Directories.Resources, null, 0, 0)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\", @$"{MsappArchive.Directories.Resources}\abc.txt" }, null, null, 1, 2)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\", @$"{MsappArchive.Directories.Resources}\abc.txt" }, null, ".txt", 1, 2)]
    [DataRow(new string[] { "abc.txt", "def.txt", @$"{MsappArchive.Directories.Resources}\", @$"{MsappArchive.Directories.Resources}\abc.txt" }, null, ".txt", 2, 3)]
    [DataRow(new string[] { "abc.jpg", @$"{MsappArchive.Directories.Resources}\", @$"{MsappArchive.Directories.Resources}\abc.txt" }, null, ".txt", 0, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" }, MsappArchive.Directories.Resources, null, 1, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" }, $@"  {MsappArchive.Directories.Resources}/  ", ".txt", 1, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}/abc.txt", @$"{MsappArchive.Directories.Resources}/qwe.jpg" },
        $@" {MsappArchive.Directories.Resources}/", ".jpg", 1, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}/abc.txt" }, $@" {MsappArchive.Directories.Resources}\", null, 1, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" }, "NotFound", "*.txt", 0, 0)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.Resources}\abc.txt",
        @$"ReSoUrCeS/efg.txt"}, MsappArchive.Directories.Resources, null, 2, 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.Resources}\abc.txt",
        @$"{MsappArchive.Directories.Resources}/efg.txt"}, "RESOURCES", null, 2, 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.Resources}New\abc.txt",
        @$"{MsappArchive.Directories.Resources}/efg.txt"}, MsappArchive.Directories.Resources, null, 1, 1)]
    [TestMethod]
    public void GetDirectoryEntriesTests(string[] entries, string directoryName, string extension, int expectedCount, int expectedRecursiveCount)
    {
        // Arrange: Create new ZipArchive in memory
        using var stream = new MemoryStream();
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true);
        foreach (var entry in entries)
        {
            zipArchive.CreateEntry(entry);
        }
        zipArchive.Dispose();

        // Act: Open the archive as MsappArchive
        stream.Position = 0;
        using var msappArchive = MsappArchiveFactory.Open(stream);

        // Assert
        msappArchive.GetDirectoryEntries(directoryName, extension, false).Count().Should().Be(expectedCount);
        msappArchive.GetDirectoryEntries(directoryName, extension, true).Count().Should().Be(expectedRecursiveCount);
    }

    [DataTestMethod]
    [DynamicData(nameof(AddEntryTestsData), DynamicDataSourceType.Method)]
    public void AddEntryTests(string[] entries, string[] expectedEntries)
    {
        // Arrange: Create new ZipArchive in memory
        using var stream = new MemoryStream();
        using var msappArchive = MsappArchiveFactory.Create(stream);
        foreach (var entry in entries)
        {
            msappArchive.CreateEntry(entry).Should().NotBeNull();
        }

        // Assert
        msappArchive.CanonicalEntries.Count.Should().Be(entries.Length);
        foreach (var expectedEntry in expectedEntries)
        {
            msappArchive.DoesEntryExist(expectedEntry).Should().BeTrue($"Expected entry {expectedEntry} to exist in the archive");
        }

        // Get the required entry should throw if it doesn't exist
        msappArchive.Invoking(a => a.GetRequiredEntry("not-exist")).Should().Throw<PersistenceLibraryException>()
            .WithErrorCode(PersistenceErrorCode.MsappArchiveError);
        msappArchive.TryGetEntry("not-exist", out var _).Should().BeFalse();
    }

    private static IEnumerable<object[]> AddEntryTestsData()
    {
        yield return new string[][] {
            [ "abc.txt" ],
            [ "abc.txt" ]
        };
        yield return new string[][] {
            [ "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" ],
            [ "abc.txt", @$"{MsappArchive.Directories.Resources}/abc.txt".ToLowerInvariant() ],
        };
        yield return new string[][] {
            [ "abc.txt", @$"{MsappArchive.Directories.Resources}\DEF.txt" ],
            [ "abc.txt", @$"{MsappArchive.Directories.Resources}/DEF.txt".ToLowerInvariant() ],
        };
        yield return new string[][] {
            [ "abc.txt", @$"{MsappArchive.Directories.Resources}\DEF.txt", @"\start-with-slash\test.json" ],
            [ "abc.txt", @$"{MsappArchive.Directories.Resources}/DEF.txt".ToLowerInvariant(), @"start-with-slash/test.json" ],
        };
    }

    [TestMethod]
    [DataRow(@"_TestData/AppsWithYaml/HelloWorld.msapp", 12, 1, "HelloScreen", "screen", 8)]
    public void Msapp_ShouldHave_Screens(string testDirectory, int allEntriesCount, int controlsCount,
        string topLevelControlName, string topLevelControlType,
        int topLevelRulesCount)
    {
        // Zip archive in memory from folder
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var files = Directory.GetFiles(testDirectory, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                zipArchive.CreateEntryFromFile(file, file.Substring(testDirectory.Length + 1));
            }
        }

        // Arrange & Act
        using var msappArchive = MsappArchiveFactory.Open(stream);

        // Assert
        msappArchive.CanonicalEntries.Count.Should().Be(allEntriesCount);
        msappArchive.App.Should().NotBeNull();
        msappArchive.App!.Screens.Count.Should().Be(controlsCount);
        msappArchive.Version.Should().Be(Version.Parse("2.2"));

        var screen = msappArchive.App.Screens.Single(c => c.Name == topLevelControlName);
    }

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
    public void DoesEntryExistTests()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var archive = new MsappArchive(archiveMemStream, ZipArchiveMode.Create, _mockYamlSerializationFactory.Object);
        archive.CreateEntry("entryA");
        archive.CreateEntry("entryB");
        archive.CreateEntry("dir1/entryA");
        archive.CreateEntry("dir1/entryB");
        archive.CreateEntry("dir2/entryA");
        archive.CreateEntry("dir2/entryC");

        // Test for entries that should exist, exact case
        archive.DoesEntryExist("entryA").Should().BeTrue();
        archive.DoesEntryExist("entryB").Should().BeTrue();
        archive.DoesEntryExist("dir1/entryA").Should().BeTrue();
        archive.DoesEntryExist("dir1/entryB").Should().BeTrue();
        archive.DoesEntryExist("dir2/entryA").Should().BeTrue();
        archive.DoesEntryExist("dir2/entryC").Should().BeTrue();

        // Should exist, but not exact case or may use non-normalized path
        archive.DoesEntryExist("ENTRYa").Should().BeTrue();
        archive.DoesEntryExist("entryB").Should().BeTrue();
        archive.DoesEntryExist("dir1/entryA").Should().BeTrue();
        archive.DoesEntryExist("Dir1\\ENTRYa").Should().BeTrue();
        archive.DoesEntryExist("dir1/entryb").Should().BeTrue();
        archive.DoesEntryExist("dir1\\entryb").Should().BeTrue();

        // Test for entries that should not exist
        archive.DoesEntryExist("entryC").Should().BeFalse();
        archive.DoesEntryExist("entryC").Should().BeFalse();
    }

    [TestMethod]
    public void DoesEntryExistWorksWithNewEntriesCreated()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var archive = new MsappArchive(archiveMemStream, ZipArchiveMode.Create, _mockYamlSerializationFactory.Object);
        archive.CreateEntry("entryA");
        archive.CreateEntry("entryB");
        archive.CreateEntry("dir2/entryA");
        archive.CreateEntry("dir2/entryC");

        // Make sure our new entry does not exist, can be created, and then exists
        archive.DoesEntryExist("dir1/newEntryD").Should().BeFalse();
        archive.CreateEntry("dir1/newEntryD");
        archive.DoesEntryExist("dir1/newEntryD").Should().BeTrue();
    }

    [TestMethod]
    public void GenerateUniqueEntryPathTests()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var archive = new MsappArchive(archiveMemStream, ZipArchiveMode.Create, _mockYamlSerializationFactory.Object);
        archive.CreateEntry("entryA.pa.yaml");
        archive.CreateEntry("entryB.pa.yaml");
        archive.CreateEntry("dir1/entryC.pa.yaml");
        archive.CreateEntry("dir1/entryD.pa.yaml");

        //
        archive.GenerateUniqueEntryPath(null, "entryA", ".pa.yaml").Should().Be("entryA1.pa.yaml");
        archive.GenerateUniqueEntryPath(null, "entryC", ".pa.yaml").Should().Be("entryC.pa.yaml");
        archive.GenerateUniqueEntryPath("dir1", "entryA", ".pa.yaml").Should().Be(Path.Combine("dir1", "entryA.pa.yaml"));
        archive.GenerateUniqueEntryPath("dir1", "entryC", ".pa.yaml").Should().Be(Path.Combine("dir1", "entryC1.pa.yaml"));

        // Verify repeated calls will keep incrementing the suffix
        var actualEntryPath = archive.GenerateUniqueEntryPath(null, "entryA", ".pa.yaml").Should().Be("entryA1.pa.yaml").And.Subject;
        archive.CreateEntry(actualEntryPath!);

        actualEntryPath = archive.GenerateUniqueEntryPath(null, "entryA", ".pa.yaml").Should().Be("entryA2.pa.yaml").And.Subject;
        archive.CreateEntry(actualEntryPath!);

        actualEntryPath = archive.GenerateUniqueEntryPath(null, "entryA", ".pa.yaml").Should().Be("entryA3.pa.yaml").And.Subject;

        // Verify when using a custom separator
        archive.GenerateUniqueEntryPath(null, "entryA", ".pa.yaml", uniqueSuffixSeparator: "_").Should().Be("entryA_1.pa.yaml");
        archive.GenerateUniqueEntryPath("dir1", "entryA", ".pa.yaml", uniqueSuffixSeparator: "_").Should().Be(Path.Combine("dir1", "entryA.pa.yaml"));
    }

    [TestMethod]
    public void GenerateUniqueEntryPathReturnsNormalizedPathsTests()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var archive = new MsappArchive(archiveMemStream, ZipArchiveMode.Create, _mockYamlSerializationFactory.Object);
        archive.CreateEntry("entryA.pa.yaml");
        archive.CreateEntry("dir1/entryA.pa.yaml");
        archive.CreateEntry("dir1/dir2/entryA.pa.yaml");

        // when entry already unique
        archive.GenerateUniqueEntryPath(null, "entryC", ".pa.yaml").Should().Be("entryC.pa.yaml");
        archive.GenerateUniqueEntryPath(@"/dir1\", "entryC", ".pa.yaml").Should().Be(Path.Combine("dir1", "entryC.pa.yaml"));
        archive.GenerateUniqueEntryPath(@"\dir1/dir2\", "entryC", ".pa.yaml").Should().Be(Path.Combine("dir1", "dir2", "entryC.pa.yaml"));

        // when unique entry generated
        archive.GenerateUniqueEntryPath(null, "entryA", ".pa.yaml").Should().Be("entryA1.pa.yaml");
        archive.GenerateUniqueEntryPath("dir1", "entryA", ".pa.yaml").Should().Be(Path.Combine("dir1", "entryA1.pa.yaml"));
        archive.GenerateUniqueEntryPath(@"/dir1\", "entryA", ".pa.yaml").Should().Be(Path.Combine("dir1", "entryA1.pa.yaml"));
        archive.GenerateUniqueEntryPath(@"\dir1/dir2\", "entryA", ".pa.yaml").Should().Be(Path.Combine("dir1", "dir2", "entryA1.pa.yaml"));
    }

    [TestMethod]
    public void NormalizeDirectoryEntryPathTests()
    {
        // Root paths:
        MsappArchive.NormalizeDirectoryEntryPath(null).Should().Be(string.Empty, "Normalized directory entry paths are used to compose full paths, and we don't want to return null");
        MsappArchive.NormalizeDirectoryEntryPath(string.Empty).Should().Be(string.Empty, "Empty string should be returned as is");
        MsappArchive.NormalizeDirectoryEntryPath("/").Should().Be(string.Empty, "Root directory should be normalized to empty string");
        MsappArchive.NormalizeDirectoryEntryPath("\\").Should().Be(string.Empty, "Root directory should be normalized to empty string");

        var expectedDir1 = $"dir1{Path.DirectorySeparatorChar}";
        var expectedDir1Dir2 = $"dir1{Path.DirectorySeparatorChar}dir2{Path.DirectorySeparatorChar}";

        // Single directory:
        MsappArchive.NormalizeDirectoryEntryPath(@"dir1").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"dir1/").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"/dir1").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"/dir1/").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"dir1\").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"\dir1").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"\dir1\").Should().Be(expectedDir1);

        // Multiple directories:
        MsappArchive.NormalizeDirectoryEntryPath(@"dir1/dir2").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"dir1/dir2/").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"/dir1/dir2").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"/dir1/dir2/").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"dir1\dir2").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"dir1\dir2\").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"\dir1\dir2").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"\dir1\dir2\").Should().Be(expectedDir1Dir2);

        // middle directory separator chars are consolidated to one:
        MsappArchive.NormalizeDirectoryEntryPath(@"//dir1//").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"\\dir1\\").Should().Be(expectedDir1);
        MsappArchive.NormalizeDirectoryEntryPath(@"//dir1/dir2//").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"\\dir1\dir2\\").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"//dir1///dir2//").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"\\dir1\\\dir2\\").Should().Be(expectedDir1Dir2);
        MsappArchive.NormalizeDirectoryEntryPath(@"\/dir1/\dir2/\").Should().Be(expectedDir1Dir2);

        // When path segment names have leading/trailing whitespace, they are currently preserved:
        MsappArchive.NormalizeDirectoryEntryPath(@"  \/  dir1  /\  dir2  /\  ")
            .Should().Be($"  {Path.DirectorySeparatorChar}  dir1  {Path.DirectorySeparatorChar}  dir2  {Path.DirectorySeparatorChar}  {Path.DirectorySeparatorChar}",
            "currently, normalization doesn't 'trim' path segment names");
    }

    [TestMethod]
    [DataRow(":%/\\?!", false, DisplayName = "Unsafe chars only")]
    [DataRow("  :%/\\  ?!  ", false, DisplayName = "Unsafe and whitespace chars only")]
    [DataRow("", false, DisplayName = "empty string")]
    [DataRow("      ", false, DisplayName = "whitespace chars only")]
    [DataRow("Foo.Bar", true, "Foo.Bar")]
    [DataRow("  Foo Bar  ", true, "Foo Bar", DisplayName = "with leading/trailing whitespace")]
    [DataRow("Foo:%/\\-?!Bar", true, "Foo-Bar")]
    public void TryMakeSafeForEntryPathSegmentWithDefaultReplacementTests(string unsafeName, bool expectedReturn, string? expectedSafeName = null)
    {
        MsappArchive.TryMakeSafeForEntryPathSegment(unsafeName, out var safeName).Should().Be(expectedReturn);
        if (expectedReturn)
        {
            safeName.ShouldNotBeNull();
            if (expectedSafeName != null)
            {
                safeName.Should().Be(expectedSafeName);
            }
        }
        else
        {
            safeName.Should().BeNull();
        }
    }

    [TestMethod]
    [DataRow(":%/\\?!", true, "______", DisplayName = "Unsafe chars only")]
    [DataRow("  :%/\\  ?!  ", true, "____  __", DisplayName = "Unsafe and whitespace chars only")]
    [DataRow("", false, DisplayName = "empty string")]
    [DataRow("      ", false, DisplayName = "whitespace chars only")]
    [DataRow("Foo.Bar", true, "Foo.Bar")]
    [DataRow("  Foo Bar  ", true, "Foo Bar", DisplayName = "with leading/trailing whitespace")]
    [DataRow("Foo:%/\\-?!Bar", true, "Foo____-__Bar")]
    public void TryMakeSafeForEntryPathSegmentWithUnderscoreReplacementTests(string unsafeName, bool expectedReturn, string? expectedSafeName = null)
    {
        MsappArchive.TryMakeSafeForEntryPathSegment(unsafeName, out var safeName, unsafeCharReplacementText: "_").Should().Be(expectedReturn);
        if (expectedReturn)
        {
            safeName.ShouldNotBeNull();
            if (expectedSafeName != null)
            {
                safeName.Should().Be(expectedSafeName);
            }
        }
        else
        {
            safeName.Should().BeNull();
        }
    }

    [TestMethod]
    public void TryMakeSafeForEntryPathSegmentWhereInputContainsPathSeparatorCharsTests()
    {
        MsappArchive.TryMakeSafeForEntryPathSegment("Foo\\Bar.pa.yaml", out var safeName).Should().BeTrue();
        safeName.Should().Be("FooBar.pa.yaml");
        MsappArchive.TryMakeSafeForEntryPathSegment("Foo/Bar.pa.yaml", out safeName).Should().BeTrue();
        safeName.Should().Be("FooBar.pa.yaml");

        // with replacement
        MsappArchive.TryMakeSafeForEntryPathSegment("Foo\\Bar.pa.yaml", out safeName, unsafeCharReplacementText: "_").Should().BeTrue();
        safeName.Should().Be("Foo_Bar.pa.yaml");
        MsappArchive.TryMakeSafeForEntryPathSegment("Foo/Bar.pa.yaml", out safeName, unsafeCharReplacementText: "-").Should().BeTrue();
        safeName.Should().Be("Foo-Bar.pa.yaml");
    }

    [TestMethod]
    public void TryMakeSafeForEntryPathSegmentWhereInputContainsInvalidPathCharTests()
    {
        var invalidChars = Path.GetInvalidPathChars()
            .Union(Path.GetInvalidFileNameChars());
        foreach (var c in invalidChars)
        {
            // Default behavior should remove invalid chars
            MsappArchive.TryMakeSafeForEntryPathSegment($"Foo{c}Bar.pa.yaml", out var safeName).Should().BeTrue();
            safeName.Should().Be("FooBar.pa.yaml");

            // Replacement char should be used for invalid chars
            MsappArchive.TryMakeSafeForEntryPathSegment($"Foo{c}Bar.pa.yaml", out safeName, unsafeCharReplacementText: "_").Should().BeTrue();
            safeName.Should().Be("Foo_Bar.pa.yaml");

            // When input results in only whitespace or empty, return value should be false
            MsappArchive.TryMakeSafeForEntryPathSegment($"{c}", out _).Should().BeFalse("because safe segment is empty string");
            MsappArchive.TryMakeSafeForEntryPathSegment($" {c} ", out _).Should().BeFalse("because safe segment is whitespace");
            MsappArchive.TryMakeSafeForEntryPathSegment($"{c} {c}", out _).Should().BeFalse("because safe segment is whitespace");
        }
    }

    [TestMethod]
    public void IsSafeForEntryPathSegmentTests()
    {
        MsappArchive.IsSafeForEntryPathSegment("Foo.pa.yaml").Should().BeTrue();

        // Path separator chars should not be used for path segments
        MsappArchive.IsSafeForEntryPathSegment("Foo/Bar.pa.yaml").Should().BeFalse("separator chars should not be used for path segments");
        MsappArchive.IsSafeForEntryPathSegment("/Foo.pa.yaml").Should().BeFalse("separator chars should not be used for path segments");
        MsappArchive.IsSafeForEntryPathSegment("Foo\\Bar.pa.yaml").Should().BeFalse("separator chars should not be used for path segments");
        MsappArchive.IsSafeForEntryPathSegment("\\Foo.pa.yaml").Should().BeFalse("separator chars should not be used for path segments");

        MsappArchive.IsSafeForEntryPathSegment("Foo/Bar.pa.yaml").Should().BeFalse("separator chars should not be used for path segments");
    }

    [TestMethod]
    public void IsSafeForEntryPathSegmentShouldNotAllowInvalidPathCharsTests()
    {
        var invalidChars = Path.GetInvalidPathChars()
            .Union(Path.GetInvalidFileNameChars());

        foreach (var c in invalidChars)
        {
            MsappArchive.IsSafeForEntryPathSegment($"Foo{c}Bar.pa.yaml").Should().BeFalse($"Invalid char '{c}' should not be allowed for path segments");
        }
    }
}
