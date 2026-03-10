// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using System.IO.Compression;

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

    private static void SaveNewMinMsappWithHeaderOnly(MemoryStream archiveStream, string headerFileName = "Header-DocV-1.347.json")
    {
        // Create an msapp-like archive with minimum required content. For this test, it's just the header.json file.
        using var writeToArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true);
        var headerFilePath = Path.Combine("_TestData", "headers", headerFileName);
        writeToArchive.CreateEntryFromFile(headerFilePath, "Header.json");
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

        MsappArchive.IsSafeForEntryPathSegment("Foo/\t.pa.yaml").Should().BeFalse("control chars should not be allowed");

        // Currently, chars outside of ascii range are not allowed
        MsappArchive.IsSafeForEntryPathSegment("Foo/éñü.pa.yaml").Should().BeFalse("latin chars are currently not allowed");
        MsappArchive.IsSafeForEntryPathSegment("Foo/あア.pa.yaml").Should().BeFalse("Japanese chars are currently not allowed");
        MsappArchive.IsSafeForEntryPathSegment("Foo/中文.pa.yaml").Should().BeFalse("CJK chars are currently not allowed");
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
