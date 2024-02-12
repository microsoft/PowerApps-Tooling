// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Persistence.Tests.MsApp;

[TestClass]
public class MsappArchiveTests : TestBase
{
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
        for (var i = 0; i != expectedEntries.Length; ++i)
        {
            msappArchive.CanonicalEntries.ContainsKey(expectedEntries[i])
                .Should()
                .BeTrue($"Expected entry {expectedEntries[i]} to exist in the archive");
        }

        // Get the required entry should throw if it doesn't exist
        var action = () => msappArchive.GetRequiredEntry("not-exist");
        action.Invoking(a => a()).Should().Throw<FileNotFoundException>();
    }

    private static IEnumerable<object[]> AddEntryTestsData()
    {
        return new[] {
            new [] {
                new[] { "abc.txt" },
                new[] { "abc.txt" }
            },
            new[]{
                new [] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" },
                new [] { "abc.txt", @$"{MsappArchive.Directories.Resources}/abc.txt".ToLowerInvariant() },
            },
            new[]{
                new [] { "abc.txt", @$"{MsappArchive.Directories.Resources}\DEF.txt" },
                new [] { "abc.txt", @$"{MsappArchive.Directories.Resources}/DEF.txt".ToLowerInvariant() },
            },
            new[]{
                new [] { "abc.txt", @$"{MsappArchive.Directories.Resources}\DEF.txt", @"\start-with-slash\test.json" },
                new [] { "abc.txt", @$"{MsappArchive.Directories.Resources}/DEF.txt".ToLowerInvariant(), @"start-with-slash/test.json" },
            }
        };
    }

    [TestMethod]
    [DataRow(@"_TestData/AppsWithYaml/HelloWorld.msapp", 14, 1, "HelloScreen", "screen", 8)]
    public void Msapp_ShouldHave_Screens(string testFile, int allEntriesCount, int controlsCount,
        string topLevelControlName, string topLevelControlType,
        int topLevelRulesCount)
    {
        // Arrange & Act
        using var msappArchive = MsappArchiveFactory.Open(testFile);

        // Assert
        msappArchive.CanonicalEntries.Count.Should().Be(allEntriesCount);
        msappArchive.App.Should().NotBeNull();
        msappArchive.App!.Screens.Count.Should().Be(controlsCount);

        var screen = msappArchive.App.Screens.Single(c => c.Name == topLevelControlName);
    }
}
