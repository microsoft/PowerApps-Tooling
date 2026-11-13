// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;
using System.IO.Compression;
using MsappArchiveFactoryImpl = Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.MsappArchiveFactory;

namespace Persistence.Tests.MsApp;

[TestClass]
public class MsappArchiveTests : TestBase
{
    public IMsappArchiveFactory MsappArchiveFactory { get; }

    public MsappArchiveTests()
    {
        var serviceProvider = BuildServiceProvider();
        MsappArchiveFactory = serviceProvider.GetRequiredService<IMsappArchiveFactory>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMsappArchiveFactory();
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public void GenerateUniqueEntryPathTests()
    {
        // Setup test archive with a couple entries in it already
        using var archiveMemStream = new MemoryStream();
        using var archive = new MsappArchive(archiveMemStream, ZipArchiveMode.Create);
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
        using var archive = new MsappArchive(archiveMemStream, ZipArchiveMode.Create);
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
    [DataRow("Header-DocV-1.250.json", null)] // MSAppStructureVersion is not set for legacy docs, but we should normalize to correct semantic value
    [DataRow("Header-DocV-1.285.json", "2.0")]
    [DataRow("Header-DocV-1.347.json", "2.4.0")]
    [DataRow("Header-DocV-1.347-SavedDate-missing.json", "2.4.0", true)]
    [DataRow("Header-DocV-1.347-SavedDate-null.json", "2.4.0", true)]
    [DataRow("Header-DocV-1.347-withUnexpectedProp.json", "2.4.0")] // This should parse, and not fail due to unexpected property
    public void HeaderParseTests(string headerFileName, string? expectedMsappStructureVersionString, bool expectSavedDateNull = false)
    {
        var expectedMsappStructureVersion = expectedMsappStructureVersionString is null ? null : Version.Parse(expectedMsappStructureVersionString);
        TestContext.WriteLine($"Expected ver: {expectedMsappStructureVersion};");
        using var archiveStream = new MemoryStream();
        SaveNewMinMsappWithHeaderOnly(archiveStream, headerFileName);

        // Read the archive using MsappArchive
        using var msappArchive = new MsappArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        var header = msappArchive.Header;
        header.MSAppStructureVersion.Should().Be(expectedMsappStructureVersion);
        if (expectSavedDateNull)
        {
            header.LastSavedDateTimeUTC.Should().BeNull();
        }
        else
        {
            header.LastSavedDateTimeUTC.Should().HaveValue()
                .And.Subject!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }
    }

    [TestMethod]
    [DataRow("Header-DocV-1.250.json", "1.0")] // MSAppStructureVersion is not set for legacy docs, but we should normalize to correct semantic value
    [DataRow("Header-DocV-1.285.json", "2.0")]
    [DataRow("Header-DocV-1.347.json", "2.4.0")]
    [DataRow("Header-DocV-1.347-SavedDate-missing.json", "2.4.0")]
    [DataRow("Header-DocV-1.347-SavedDate-null.json", "2.4.0")]
    public void MSAppStructureVersionTests(string headerFileName, string expectedMsappStructureVersionString)
    {
        var expectedMsappStructureVersion = Version.Parse(expectedMsappStructureVersionString);
        using var archiveStream = new MemoryStream();
        SaveNewMinMsappWithHeaderOnly(archiveStream, headerFileName);

        // Read the archive using MsappArchive
        using var msappArchive = new MsappArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        msappArchive.MSAppStructureVersion.Should().Be(expectedMsappStructureVersion);
    }

    [TestMethod]
    public void FactoryWithoutLimitsDoesNotCheckLimits()
    {
        using var archiveStream = new MemoryStream();
        SaveNewMinMsappWithHeaderOnly(archiveStream);

        MsappArchiveFactory.Should().BeOfType<MsappArchiveFactoryImpl>().Which.Limits.Should().BeNull("AddMsappArchiveFactory() registers a factory without limits");
        MsappArchiveFactoryImpl.Default.Limits.Should().BeNull();
        using var msappArchive = MsappArchiveFactoryImpl.Default.Open(archiveStream, leaveOpen: true);
        msappArchive.Limits.Should().BeNull();
        msappArchive.DocVersion.Should().NotBeNull();
    }

    [TestMethod]
    public void FactoryWithLimitsChecksOpenAndUpdateButNotCreate()
    {
        var limits = PaArchiveLimits.Default with { MaxTextEntrySize = 1 };
        var factory = new MsappArchiveFactoryImpl { Limits = limits };
        factory.Limits.Should().BeSameAs(limits);

        using var archiveStream = new MemoryStream();
        SaveNewMinMsappWithHeaderOnly(archiveStream);
        var archiveBytes = archiveStream.ToArray();

        factory.Invoking(f => f.Open(archiveStream, leaveOpen: true))
            .Should().ThrowExactly<PaArchiveLimitExceededException>()
            .Which.Limit.Should().Be(PaArchiveLimit.TextEntrySize);
        factory.Invoking(f => f.Update(archiveStream, leaveOpen: true))
            .Should().ThrowExactly<PaArchiveLimitExceededException>()
            .Which.Limit.Should().Be(PaArchiveLimit.TextEntrySize);
        archiveStream.ToArray().Should().Equal(archiveBytes);

        using var created = factory.Create(new MemoryStream());
        created.Limits.Should().BeSameAs(limits);
        using var writer = new StreamWriter(created.CreateEntry("Header.json").Open());
        writer.Write("{}");
    }

    [TestMethod]
    public void FactoryWithLimitsRejectsFileWithoutModifyingOrLockingIt()
    {
        var path = Path.Combine(CreateTestOutputFolder(ensureEmpty: true), "app.msapp");
        using (var fileStream = File.Create(path))
        using (var writeToArchive = new ZipArchive(fileStream, ZipArchiveMode.Create))
        {
            writeToArchive.CreateEntryFromFile(Path.Combine("_TestData", "headers", "Header-DocV-1.347.json"), "Header.json");
        }
        var originalBytes = File.ReadAllBytes(path);
        var factory = new MsappArchiveFactoryImpl { Limits = PaArchiveLimits.Default with { MaxTextEntrySize = 1 } };

        factory.Invoking(f => f.Update(path)).Should().ThrowExactly<PaArchiveLimitExceededException>();
        File.ReadAllBytes(path).Should().Equal(originalBytes, "a rejected update should not rewrite the file");
        using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // The file is no longer in use by the rejected archive
        }

        factory.Invoking(f => f.Open(path)).Should().ThrowExactly<PaArchiveLimitExceededException>();
        using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // The file is no longer in use by the rejected archive
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddMsappArchiveFactoryWithLimitsRegistersFactoryWithLimits(bool addPackingServiceFirst)
    {
        var limits = PaArchiveLimits.Default with { MaxEntryCount = 5 };
        var services = new ServiceCollection();
        if (addPackingServiceFirst)
            services.AddMsappPackingService();
        services.AddMsappArchiveFactory(limits);
        if (!addPackingServiceFirst)
            services.AddMsappPackingService();
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<IMsappArchiveFactory>()
            .Should().BeOfType<MsappArchiveFactoryImpl>()
            .Which.Limits.Should().BeSameAs(limits, "limits passed explicitly should apply regardless of registration order");
        services.Where(d => d.ServiceType == typeof(IMsappArchiveFactory)).Should().ContainSingle();
    }

    [TestMethod]
    public void AddMsappArchiveFactoryIgnoresRegisteredLimits()
    {
        var services = new ServiceCollection();
        services.AddSingleton(PaArchiveLimits.Default);
        services.AddMsappArchiveFactory();
        services.TryAddSingleton<MsappArchiveFactoryImpl>();
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<IMsappArchiveFactory>()
            .Should().BeOfType<MsappArchiveFactoryImpl>()
            .Which.Limits.Should().BeNull("limits are only applied when passed explicitly");
        serviceProvider.GetRequiredService<MsappArchiveFactoryImpl>().Limits.Should().BeNull("limits are only applied when passed explicitly");
    }

    private static void SaveNewMinMsappWithHeaderOnly(MemoryStream archiveStream, string headerFileName = "Header-DocV-1.347.json")
    {
        // Create an msapp-like archive with minimum required content. For this test, it's just the header.json file.
        using var writeToArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true);
        var headerFilePath = Path.Combine("_TestData", "headers", headerFileName);
        writeToArchive.CreateEntryFromFile(headerFilePath, "Header.json");
    }
}
