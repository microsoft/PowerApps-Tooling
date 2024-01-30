// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.MsApp;

[TestClass]
public class MsappArchiveTests
{
    [DataRow(new string[] { "abc.txt" }, MsappArchive.Directories.Resources, 0)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" }, MsappArchive.Directories.Resources, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" }, $@" \{MsappArchive.Directories.Resources}/", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}/abc.txt" }, $@" {MsappArchive.Directories.Resources}/", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}/abc.txt" }, $@" {MsappArchive.Directories.Resources}\", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" }, "NotFound", 0)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.Resources}\abc.txt",
        @$"ReSoUrCeS/efg.txt"}, MsappArchive.Directories.Resources, 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.Resources}\abc.txt",
        @$"{MsappArchive.Directories.Resources}/efg.txt"}, "RESOURCES", 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.Resources}New\abc.txt",
        @$"{MsappArchive.Directories.Resources}/efg.txt"}, MsappArchive.Directories.Resources, 1)]
    [TestMethod]
    public void GetDirectoryEntriesTests(string[] entries, string directoryName, int expectedDirectoryCount)
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
        using var msappArchive = new MsappArchive(stream);

        // Assert
        msappArchive.GetDirectoryEntries(directoryName).Count().Should().Be(expectedDirectoryCount);
    }

    [DataTestMethod]
    [DynamicData(nameof(AddEntryTestsData), DynamicDataSourceType.Method)]
    public void AddEntryTests(string[] entries, string[] expectedEntries)
    {
        // Arrange: Create new ZipArchive in memory
        using var stream = new MemoryStream();
        using var msappArchive = new MsappArchive(stream, ZipArchiveMode.Create);
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
        // Arrange: Create new ZipArchive in memory
        using var msappArchive = new MsappArchive(testFile, YamlSerializationFactory.CreateDeserializer());

        // Assert
        msappArchive.CanonicalEntries.Count.Should().Be(allEntriesCount);
        msappArchive.App.Screens.Count.Should().Be(controlsCount);

        var screen = msappArchive.App.Screens.Single(c => c.Name == topLevelControlName);
    }
}
