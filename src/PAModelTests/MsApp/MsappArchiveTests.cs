// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools.MsApp;

namespace PAModelTests.MsApp;

[TestClass]
public class MsappArchiveTests
{
    [DataRow(new string[] { "abc.txt" }, MsappArchive.ResourcesDirectory, 0)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}\abc.txt" }, MsappArchive.ResourcesDirectory, 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}\abc.txt" }, $@" \{MsappArchive.ResourcesDirectory}/", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}/abc.txt" }, $@" {MsappArchive.ResourcesDirectory}/", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}/abc.txt" }, $@" {MsappArchive.ResourcesDirectory}\", 1)]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}\abc.txt" }, "NotFound", 0)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.ResourcesDirectory}\abc.txt",
        @$"ReSoUrCeS/efg.txt"}, MsappArchive.ResourcesDirectory, 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.ResourcesDirectory}\abc.txt",
        @$"{MsappArchive.ResourcesDirectory}/efg.txt"}, "RESOURCES", 2)]
    [DataRow(new string[] {"abc.txt",
        @$"{MsappArchive.ResourcesDirectory}New\abc.txt",
        @$"{MsappArchive.ResourcesDirectory}/efg.txt"}, MsappArchive.ResourcesDirectory, 1)]
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
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}\abc.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}\DEF.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.ResourcesDirectory}\DEF.txt", @"\start-with-slash\test.json" })]
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
            msappArchive.CanonicalEntries.ContainsKey(MsappArchive.NormalizePath(entry)).Should().BeTrue();
        }

        // Get the required entry should throw if it doesn't exist
        var action = () => msappArchive.GetRequiredEntry("not-exist");
        action.Invoking(a => a()).Should().Throw<FileNotFoundException>();
    }

    [TestMethod]
    [DataRow(@"Apps/WithYaml/HelloWorld.msapp", 14, 2, "HelloScreen", 8)]
    [DataRow(@"Apps/AppWithLabel.msapp", 11, 2, "Screen1", 8)]
    public void GetTopLevelControlsTests(string testFile, int allEntriesCount, int controlsCount, string topLevelControlName, int topLevelRulesCount)
    {
        // Arrange: Create new ZipArchive in memory
        using var msappArchive = new MsappArchive(testFile);

        // Assert
        msappArchive.CanonicalEntries.Count.Should().Be(allEntriesCount);
        msappArchive.TopLevelControls.Count.Should().Be(controlsCount);
        msappArchive.TopLevelControls.Should().ContainSingle(c => c.Name == "App");

        var topLevelControl = msappArchive.TopLevelControls.Single(c => c.Name == topLevelControlName);
        topLevelControl.EditorState.Rules.Count.Should().Be(topLevelRulesCount);
    }
}
