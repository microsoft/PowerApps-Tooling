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
            msappArchive.CanonicalEntries.ContainsKey(expectedEntry)
                .Should()
                .BeTrue($"Expected entry {expectedEntry} to exist in the archive");
            msappArchive.DoesEntryExist(expectedEntry).Should().BeTrue();
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
    public void TryGenerateUniqueEntryPathTests()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var archive = new MsappArchive(archiveMemStream, ZipArchiveMode.Create, _mockYamlSerializationFactory.Object);
        archive.CreateEntry("entryA.pa.yaml");
        archive.CreateEntry("entryB.pa.yaml");
        archive.CreateEntry("dir1/entryC.pa.yaml");
        archive.CreateEntry("dir1/entryD.pa.yaml");

        //
        string? actualEntryPath;
        archive.TryGenerateUniqueEntryPath(null, "entryA", ".pa.yaml", out actualEntryPath).Should().BeTrue();
        actualEntryPath.Should().Be("entryA1.pa.yaml");
        archive.TryGenerateUniqueEntryPath(null, "entryC", ".pa.yaml", out actualEntryPath).Should().BeTrue();
        actualEntryPath.Should().Be("entryC.pa.yaml");
        archive.TryGenerateUniqueEntryPath("dir1", "entryA", ".pa.yaml", out actualEntryPath).Should().BeTrue();
        actualEntryPath.Should().Be("dir1\\entryA.pa.yaml");
        archive.TryGenerateUniqueEntryPath("dir1", "entryC", ".pa.yaml", out actualEntryPath).Should().BeTrue();
        actualEntryPath.Should().Be("dir1\\entryC1.pa.yaml");

        // Verify repeated calls will keep incrementing the suffix
        archive.TryGenerateUniqueEntryPath(null, "entryA", ".pa.yaml", out actualEntryPath).Should().BeTrue();
        actualEntryPath.Should().Be("entryA1.pa.yaml");
        archive.CreateEntry(actualEntryPath!);

        archive.TryGenerateUniqueEntryPath(null, "entryA", ".pa.yaml", out actualEntryPath).Should().BeTrue();
        actualEntryPath.Should().Be("entryA2.pa.yaml");
        archive.CreateEntry(actualEntryPath!);

        archive.TryGenerateUniqueEntryPath(null, "entryA", ".pa.yaml", out actualEntryPath).Should().BeTrue();
        actualEntryPath.Should().Be("entryA3.pa.yaml");

        // Verify when using a custom separator
        archive.TryGenerateUniqueEntryPath(null, "entryA", ".pa.yaml", out actualEntryPath, uniqueSuffixSeparator: "_").Should().BeTrue();
        actualEntryPath.Should().Be("entryA_1.pa.yaml");
        archive.TryGenerateUniqueEntryPath("dir1", "entryA", ".pa.yaml", out actualEntryPath, uniqueSuffixSeparator: "_").Should().BeTrue();
        actualEntryPath.Should().Be("dir1\\entryA.pa.yaml");
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
}
