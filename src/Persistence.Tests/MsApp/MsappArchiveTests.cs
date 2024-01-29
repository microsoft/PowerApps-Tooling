// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Utils;

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

    [DataRow(new string[] { "abc.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\abc.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\DEF.txt" })]
    [DataRow(new string[] { "abc.txt", @$"{MsappArchive.Directories.Resources}\DEF.txt", @"\start-with-slash\test.json" })]
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
}
