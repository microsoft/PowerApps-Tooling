// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Persistence.Tests.MsappPacking;

[TestClass]
public class MsappPackingServiceTests : TestBase
{
    private const string AlmTestApp_asManyEntitiesAsPossible = "AlmTestApp-asManyEntitiesAsPossible.msapp";
    private const string AlmTestAppMsaprName = "AlmTestApp-asManyEntitiesAsPossible.msapr";

    private static readonly PackedJsonPackingClient TestPackingClient = new()
    {
        Name = nameof(MsappPackingServiceTests),
        Version = "0.1.0",
    };

    [TestMethod]
    public void UnpackToDirectoryWithDefaultConfig()
    {
        // Arrange
        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        var unpackedDir = Path.Combine(testDir, "unpackedOut");
        var msappPath = Path.Combine("_TestData", "AlmApps", AlmTestApp_asManyEntitiesAsPossible);
        var service = new MsappPackingService(MsappArchiveFactory.Default, MsappReferenceArchiveFactory.Default);

        // Act: unpack with default config (only PaYamlSourceCode is unpacked to disk)
        service.UnpackToDirectory(msappPath, unpackedDir);

        Directory.Exists(unpackedDir).Should().BeTrue("service should have created the output folder if it didn't already exist");

        // Assert: .msapr is created alongside the extracted source
        var msaprPath = Path.Combine(unpackedDir, Path.ChangeExtension(AlmTestApp_asManyEntitiesAsPossible, MsaprLayoutConstants.FileExtensions.Msapr));
        File.Exists(msaprPath).Should().BeTrue("the .msapr file should be created");

        // Assert: all Src/*.pa.yaml files are extracted to disk
        TestingUtilities.GetNormalizedFilePathsUnderDirectory(Path.Combine(unpackedDir, "Src"))
            .Should().Equal([
                @"Components\MyTitleComponent.pa.yaml",
                "_EditorState.pa.yaml",
                "App.pa.yaml",
                "Screen1.pa.yaml",
                ]);

        // Assert: Assets are NOT on disk (not unpacked with default config; stored in the .msapr)
        Directory.Exists(Path.Combine(unpackedDir, "Assets")).Should().BeFalse(
            "Assets should remain in the .msapr, not be extracted to disk with default config");

        using var msaprArchive = MsappReferenceArchiveFactory.Default.Open(msaprPath);
        msaprArchive.Should().ContainEntry(MsaprLayoutConstants.FileNames.MsaprHeader, "msapr header file should always be written");

        // Assert: .msapr contains the files from the msapp that were not extracted to disk.
        msaprArchive.Should()
            .ContainEntry("msapp/Header.json")
            .And.ContainEntry("msapp/Properties.json")
            .And.ContainEntry("msapp/ComponentsMetadata.json")
            .And.ContainEntry("msapp/AppCheckerResult.sarif")

            // editor state internal representation of controls and components
            .And.ContainEntry("msapp/Components/7.json")
            .And.ContainEntry("msapp/Controls/1.json")
            .And.ContainEntry("msapp/Controls/4.json")

            // Hidden entities
            .And.ContainEntry("msapp/References/Themes.json")
            .And.ContainEntry("msapp/References/DataSources.json")
            .And.ContainEntry("msapp/References/ModernThemes.json")
            .And.ContainEntry("msapp/References/QualifiedValues.json")
            .And.ContainEntry("msapp/References/Resources.json")
            .And.ContainEntry("msapp/References/Templates.json")

            // Assets are not extracted with default config, so they should be in the .msapr
            .And.ContainEntry("msapp/Assets/Images/e3a466bb-b793-4b1e-95a9-6b69efcd7b7b.jpg")
            .And.ContainEntry("msapp/Assets/Images/d60d1b08-a1f6-46e7-b305-9c4b2d4d417c.png")
            .And.ContainEntry("msapp/Assets/Images/fae39ff3-1276-4ea4-96d3-60ebee45b286.png")
            ;

        // Assert: pa.yaml files were NOT copied into the .msapr (they live on disk)
        msaprArchive.Should().NotContainAnyEntriesInDirectoryRecursive("msapp/Src");

        // Assert other directories are empty
        msaprArchive.Should()
            .HaveCountEntriesInDirectory("", expectedCount: 1, "only msapr-header.json is expeted at the root currently")
            .And.NotContainAnyEntriesInDirectoryRecursive("Assets")
            .And.NotContainAnyEntriesInDirectoryRecursive("Components")
            .And.NotContainAnyEntriesInDirectoryRecursive("Controls")
            .And.NotContainAnyEntriesInDirectoryRecursive("References")
            .And.NotContainAnyEntriesInDirectoryRecursive("Src")
            ;
    }

    [TestMethod]
    [DataRow("Header-DocV-1.250.json")]  // MSAppStructureVersion is absent (normalizes to 1.0)
    [DataRow("Header-DocV-1.285.json")]  // MSAppStructureVersion 2.0
    [DataRow("Header-DocV-1.347.json", true)]  // DocVersion 1.347
    [DataRow("Header-DocV-1.347-SavedDate-missing.json", true)]  // DocVersion 1.347
    [DataRow("Header-DocV-1.347-SavedDate-null.json", true)]  // DocVersion 1.347
    [DataRow("Header-DocV-1.347-withUnexpectedProp.json", true)]  // DocVersion 1.347
    public void ValidateMsappUnpackIsSupported_ThrowsForUnsupportedHeaderVersions(string headerFileName, bool docVersionTooEarly = false)
    {
        using var msappArchive = OpenMsappWithHeader(headerFileName);

        FluentActions.Invoking(() => MsappPackingService.ValidateMsappUnpackIsSupported(msappArchive))
            .Should().Throw<MsappUnpackException>()
            .WithMessage(docVersionTooEarly
                ? "DocVersion * is below the minimum supported version *"
                : "MSAppStructureVersion * is below the minimum supported version *");

        static MsappArchive OpenMsappWithHeader(string headerFileName)
        {
            var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                zip.CreateEntryFromFile(Path.Combine("_TestData", "headers", headerFileName), MsappLayoutConstants.FileNames.Header);
            return MsappArchiveFactory.Default.Open(ms, leaveOpen: false);
        }
    }

    [TestMethod]
    public void PackFromMsappReferenceFile_RoundTrip_ProducesSameEntries()
    {
        // Arrange
        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        var unpackedDir = Path.Combine(testDir, "unpacked");
        var repackedMsappPath = Path.Combine(testDir, "repacked.msapp");
        var msappPath = Path.Combine("_TestData", "AlmApps", AlmTestApp_asManyEntitiesAsPossible);
        var service = new MsappPackingService(MsappArchiveFactory.Default, MsappReferenceArchiveFactory.Default);

        // Act: unpack then pack
        service.UnpackToDirectory(msappPath, unpackedDir);
        var msaprPath = Path.Combine(unpackedDir, AlmTestAppMsaprName);
        service.PackFromMsappReferenceFile(msaprPath, repackedMsappPath, TestPackingClient);

        // Assert: output file exists
        File.Exists(repackedMsappPath).Should().BeTrue("the repacked .msapp file should be created");

        using var repackedMsapp = MsappArchiveFactory.Default.Open(repackedMsappPath);

        // Assert: all original entries are present in the repacked app with identical content
        using (var originalMsapp = MsappArchiveFactory.Default.Open(msappPath))
        {

            var originalEntries = originalMsapp.Entries
                .OrderBy(e => e.FullName, FilePathComparer.Instance)
                .ToList();
            foreach (var originalEntry in originalEntries)
            {
                var repackedEntry = repackedMsapp.Should().ContainEntry(originalEntry.FullName, $"repacked msapp should contain original entry").Which;
                repackedEntry.ComputeHash().Should().Be(originalEntry.ComputeHash(), $"entry '{originalEntry.FullName}' content should be identical after round-trip");
            }
        }

        // Assert: packed.json is present and correctly populated
        var packedJson = repackedMsapp.Should().ContainEntry(MsappLayoutConstants.FileNames.Packed)
            .Which.DeserializeAsJson<PackedJson>(MsappSerialization.PackedJsonSerializeOptions);
        packedJson.PackedStructureVersion.Should().Be(PackedJson.CurrentPackedStructureVersion);
        packedJson.LastPackedDateTimeUtc.Should().NotBeNull();
        packedJson.LoadConfiguration.LoadFromYaml.Should().BeTrue("PaYamlSourceCode was unpacked");
        packedJson.PackingClient.Should().BeEquivalentTo(TestPackingClient);
    }

    [TestMethod]
    public void PackFromMsappReferenceFile_ThrowsWhenOutputExists_AndOverwriteIsFalse()
    {
        // Arrange
        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        var unpackedDir = Path.Combine(testDir, "unpacked");
        var outputMsappPath = Path.Combine(testDir, "output.msapp");
        var msappPath = Path.Combine("_TestData", "AlmApps", AlmTestApp_asManyEntitiesAsPossible);
        var service = new MsappPackingService(MsappArchiveFactory.Default, MsappReferenceArchiveFactory.Default);

        service.UnpackToDirectory(msappPath, unpackedDir);
        var msaprPath = Path.Combine(unpackedDir, AlmTestAppMsaprName);

        // Create a file at the output path to simulate a conflict
        File.WriteAllText(outputMsappPath, "existing content");

        // Act & Assert
        FluentActions.Invoking(() => service.PackFromMsappReferenceFile(msaprPath, outputMsappPath, TestPackingClient, overwriteOutput: false))
            .Should().Throw<MsappPackException>()
            .WithMessage($"*'{outputMsappPath}'*");
    }

    [TestMethod]
    public void PackFromMsappReferenceFile_Overwrites_WhenOverwriteIsTrue()
    {
        // Arrange
        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        var unpackedDir = Path.Combine(testDir, "unpacked");
        var outputMsappPath = Path.Combine(testDir, "output.msapp");
        var msappPath = Path.Combine("_TestData", "AlmApps", AlmTestApp_asManyEntitiesAsPossible);
        var service = new MsappPackingService(MsappArchiveFactory.Default, MsappReferenceArchiveFactory.Default);

        service.UnpackToDirectory(msappPath, unpackedDir);
        var msaprPath = Path.Combine(unpackedDir, AlmTestAppMsaprName);

        // Create a file at the output path
        File.WriteAllText(outputMsappPath, "existing content");

        // Act: should not throw
        service.PackFromMsappReferenceFile(msaprPath, outputMsappPath, TestPackingClient, overwriteOutput: true);

        // Assert: the file was overwritten with a valid msapp
        using var msapp = MsappArchiveFactory.Default.Open(outputMsappPath);
        msapp.Entries.Should().NotBeEmpty("the overwritten file should be a valid msapp with entries");
    }

    [TestMethod]
    public void PackFromMsappReferenceFile_PreservesNonAsciiSrcFileNames()
    {
        // Arrange: unpack, then add pa.yaml files with non-ASCII names
        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        var unpackedDir = Path.Combine(testDir, "unpacked");
        var repackedMsappPath = Path.Combine(testDir, "repacked.msapp");
        var msappPath = Path.Combine("_TestData", "AlmApps", AlmTestApp_asManyEntitiesAsPossible);
        var service = new MsappPackingService(MsappArchiveFactory.Default, MsappReferenceArchiveFactory.Default);

        service.UnpackToDirectory(msappPath, unpackedDir);

        var srcDir = Path.Combine(unpackedDir, MsappLayoutConstants.DirectoryNames.Src);
        var nonAsciiFileNames = new[]
        {
            "Menú.pa.yaml",   // Spanish: ú (\u00FA)
            "Données.pa.yaml", // French: é (\u00E9)
            "画面.pa.yaml",    // Japanese: 画面 (screen)
        };
        foreach (var fileName in nonAsciiFileNames)
            File.WriteAllText(Path.Combine(srcDir, fileName), string.Empty);

        // Act
        var msaprPath = Path.Combine(unpackedDir, AlmTestAppMsaprName);
        service.PackFromMsappReferenceFile(msaprPath, repackedMsappPath, TestPackingClient);

        // Assert: each non-ASCII entry name is preserved verbatim in the packed msapp
        using var repackedMsapp = MsappArchiveFactory.Default.Open(repackedMsappPath);
        foreach (var fileName in nonAsciiFileNames)
        {
            var expectedEntryPath = Path.Combine(MsappLayoutConstants.DirectoryNames.Src, fileName);
            repackedMsapp.Should().ContainEntry(expectedEntryPath)
                .Which.Name.Should().Be(fileName, "the file name should be preserved exactly as written to disk");
        }
    }

    [TestMethod]
    public void PackFromMsappReferenceFile_IgnoresNonPaYamlFileInSrc()
    {
        // Arrange
        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        var unpackedDir = Path.Combine(testDir, "unpacked");
        var outputMsappPath = Path.Combine(testDir, "output.msapp");
        var msappPath = Path.Combine("_TestData", "AlmApps", AlmTestApp_asManyEntitiesAsPossible);
        var service = new MsappPackingService(MsappArchiveFactory.Default, MsappReferenceArchiveFactory.Default);

        service.UnpackToDirectory(msappPath, unpackedDir);

        // Add a non-.pa.yaml file to the Src folder
        File.WriteAllText(Path.Combine(unpackedDir, "Src", "notes.txt"), "This is not a pa.yaml file");

        var msaprPath = Path.Combine(unpackedDir, AlmTestAppMsaprName);

        // Act: should not throw; behavior is Ignore
        service.PackFromMsappReferenceFile(msaprPath, outputMsappPath, TestPackingClient);

        // Assert: notes.txt is NOT present in the output msapp
        using var repackedMsapp = MsappArchiveFactory.Default.Open(outputMsappPath);
        repackedMsapp.Should().NotContainEntry(Path.Combine("Src", "notes.txt"), "notes.txt is not a .pa.yaml file and should not be included in the packed msapp");
    }

    [TestMethod]
    public void BuildPackInstructions_ProducesCorrectInstructions()
    {
        // Arrange: unpack to a temp dir, then verify BuildPackInstructions output
        var testDir = CreateTestOutputFolder(ensureEmpty: true);
        var unpackedDir = Path.Combine(testDir, "unpacked");
        var msappPath = Path.Combine("_TestData", "AlmApps", AlmTestApp_asManyEntitiesAsPossible);
        var service = new MsappPackingService(MsappArchiveFactory.Default, MsappReferenceArchiveFactory.Default);

        service.UnpackToDirectory(msappPath, unpackedDir);
        var msaprPath = Path.Combine(unpackedDir, AlmTestAppMsaprName);

        using var msaprArchive = MsappReferenceArchiveFactory.Default.Open(msaprPath);
        var unpackedConfig = new UnpackedConfiguration();

        // Act
        var instructions = MsappPackingService.BuildPackInstructions(msaprArchive, unpackedDir, unpackedConfig).ToList();

        // Assert: every instruction has exactly one of the two sources set
        instructions.Should().AllSatisfy(i =>
        {
            (i.CopyFromMsaprEntry is not null ^ i.ReadFromFilePath is not null)
                .Should().BeTrue("each instruction should have exactly one source");
        });

        // Assert: instructions from msapr have no "msapp/" prefix in the target path
        var msaprDirPrefix = MsaprLayoutConstants.DirectoryNames.Msapp + Path.DirectorySeparatorChar;
        var copyEntryInstructions = instructions.Where(i => i.CopyFromMsaprEntry is not null).ToList();
        copyEntryInstructions.Should().NotBeEmpty()
            .And.AllSatisfy(i => i.MsappEntryPath.FullName.Should().NotStartWith(msaprDirPrefix, "msapp/ prefix should be stripped"));

        var srcDirPrefix = MsappLayoutConstants.DirectoryNames.Src + Path.DirectorySeparatorChar;
        var packFromDiskInstructions = instructions.Where(i => i.ReadFromFilePath is not null).ToList();
        packFromDiskInstructions.Should().NotBeEmpty("PaYamlSourceCode files should produce disk instructions")
            .And.AllSatisfy(i => File.Exists(i.ReadFromFilePath).Should().BeTrue($"disk file '{i.ReadFromFilePath}' should exist"))
            .And.AllSatisfy(i => i.MsappEntryPath.FullName.Should().StartWith(srcDirPrefix, "all disk entries should be under Src/"));
    }
}
