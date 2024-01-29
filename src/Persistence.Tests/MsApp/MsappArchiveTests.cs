// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Utils;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.MsApp;

[TestClass]
public class MsappArchiveTests
{
    [DataRow(new string[] { "abc.txt" }, MsappArchive.Directories.ResourcesDirectory, 0)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}\abc.txt" }, MsappArchive.Directories.ResourcesDirectory, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}\abc.txt" }, $@" \{MsappArchive.Directories.ResourcesDirectory}/", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}/abc.txt" }, $@" {MsappArchive.Directories.ResourcesDirectory}/", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}/abc.txt" }, $@" {MsappArchive.Directories.ResourcesDirectory}\", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}\abc.txt" }, "NotFound", 0)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.ResourcesDirectory}\abc.txt",
        @$"ReSoUrCeS/efg.txt"}, MsappArchive.Directories.ResourcesDirectory, 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.ResourcesDirectory}\abc.txt",
        @$"{MsappArchive.Directories.ResourcesDirectory}/efg.txt"}, "RESOURCES", 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.Directories.ResourcesDirectory}New\abc.txt",
        @$"{MsappArchive.Directories.ResourcesDirectory}/efg.txt"}, MsappArchive.Directories.ResourcesDirectory, 1)]
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

    [DataRow(new string[] { "abc.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}\abc.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}\DEF.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.ResourcesDirectory}\DEF.txt", @"\start-with-slash\test.json" })]
    [TestMethod]
    public void AddEntryTests(string[] entries)
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
        foreach (var entry in entries)
        {
            msappArchive.CanonicalEntries.ContainsKey(FileUtils.NormalizePath(entry)).Should().BeTrue();
        }

        // Get the required entry should throw if it doesn't exist
        var action = () => msappArchive.GetRequiredEntry("not-exist");
        action.Invoking(a => a()).Should().Throw<FileNotFoundException>();
    }

    [TestMethod]
    [DataRow(@"_TestData/AppsWithYaml/HelloWorld.msapp", 14, 2, "HelloScreen", "screen", 8)]
    public void Msapp_ShouldHave_Screens(string testFile, int allEntriesCount, int controlsCount,
        string topLevelControlName, string topLevelControlType,
        int topLevelRulesCount)
    {
        // Arrange: Create new ZipArchive in memory
        using var msappArchive = new MsappArchive(testFile, YamlSerializationFactory.CreateDeserializer());

        // Assert
        msappArchive.CanonicalEntries.Count.Should().Be(allEntriesCount);
        msappArchive.Screens.Count.Should().Be(controlsCount);
        msappArchive.Screens.Should().ContainSingle(c => c.Name == "App");

        var screen = msappArchive.Screens.Single(c => c.Name == topLevelControlName);
    }
}
