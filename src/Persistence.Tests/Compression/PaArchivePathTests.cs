// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using FluentAssertions.Execution;
using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

namespace Persistence.Tests.Compression;

[TestClass]
public class PaArchivePathTests : TestBase
{
    [TestMethod]
    [DataRow("")]
    [DataRow(@"\")]
    [DataRow(@"/")]
    [DataRow(@"\\")]
    [DataRow(@"//")]
    public void RootPathTests(string fullPath)
    {
        var archivePath = new PaArchivePath(fullPath);
        archivePath.FullName.Should().BeEmpty();
        archivePath.Name.Should().BeEmpty();
        archivePath.IsRoot.Should().BeTrue();
        archivePath.IsDirectory.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("Header.json", "Header.json", "Header.json")]
    [DataRow(".json", ".json", ".json")]
    [DataRow(@"/dir/file2.txt", @"dir\file2.txt", "file2.txt")]
    [DataRow(@"Src\some.pa.yaml", @"Src\some.pa.yaml", "some.pa.yaml")]
    [DataRow(@"Src/some.pa.yaml", @"Src\some.pa.yaml", "some.pa.yaml")]
    [DataRow(@"/Src/some.pa.yaml", @"Src\some.pa.yaml", "some.pa.yaml")]
    [DataRow(@"\Src/some.pa.yaml", @"Src\some.pa.yaml", "some.pa.yaml")]
    [DataRow(@"Src\.yaml", @"Src\.yaml", ".yaml")]
    [DataRow(@"Src/", @"Src\", "Src", true)]
    [DataRow(@"\Src/", @"Src\", "Src", true)]
    [DataRow(@"\Src\", @"Src\", "Src", true)]
    [DataRow(@"some-dir_with555.common.chars/some-dir_with555.common.chars", @"some-dir_with555.common.chars\some-dir_with555.common.chars", "some-dir_with555.common.chars")]
    public void ValidPathTest(string fullPath, string expectedRelativePathWindows, string expectedName, bool expectIsDirectory = false)
    {
        // To simplify test case inputs, we expect them to be specified using windows path separators and replace them with the current platform's chars
        var expectedRelativePath = expectedRelativePathWindows.Replace('\\', Path.DirectorySeparatorChar);

        var archivePath = new PaArchivePath(fullPath);
        archivePath.FullName.Should().Be(expectedRelativePath);
        archivePath.Name.Should().Be(expectedName);
        archivePath.IsDirectory.Should().Be(expectIsDirectory);
    }

    // Chars which are valid in OS paths and allowed here:
    [TestMethod]
    [DataRow('~')]
    [DataRow('!')]
    [DataRow('@')]
    [DataRow('#')]
    [DataRow('$')]
    [DataRow('%')]
    [DataRow('^')]
    [DataRow('&')]
    [DataRow(';')]
    [DataRow('+')]
    [DataRow('=')]
    [DataRow('`')]
    [DataRow('\'')]
    // Latin extended:
    [DataRow('é')] // LATIN SMALL LETTER E WITH ACUTE
    [DataRow('ñ')] // LATIN SMALL LETTER N WITH TILDE
    [DataRow('ü')] // LATIN SMALL LETTER U WITH DIAERESIS
    // Japanese:
    [DataRow('あ')] // HIRAGANA LETTER A
    [DataRow('ア')] // KATAKANA LETTER A
    // CJK:
    [DataRow('中')] // CJK UNIFIED IDEOGRAPH-4E2D
    [DataRow('文')] // CJK UNIFIED IDEOGRAPH-6587
    public void ValidPathWithSpecialCharsTest(char specialChar)
    {
        var fullPath = $"dir{Path.DirectorySeparatorChar}{specialChar}";

        var archivePath = new PaArchivePath(fullPath);
        archivePath.FullName.Should().Be(fullPath);
        archivePath.Name.Should().Be(specialChar.ToString());
        archivePath.IsDirectory.Should().BeFalse();
    }

    [TestMethod]
    // Single directory:
    [DataRow(@"dir1", @"dir1\", "dir1", true)]
    [DataRow(@"dir1/", @"dir1\", "dir1")]
    [DataRow(@"/dir1", @"dir1\", "dir1", true)]
    [DataRow(@"/dir1/", @"dir1\", "dir1")]
    [DataRow(@"dir1\", @"dir1\", "dir1")]
    [DataRow(@"\dir1", @"dir1\", "dir1", true)]
    [DataRow(@"\dir1\", @"dir1\", "dir1")]
    // Multiple directories:
    [DataRow(@"dir1/dir2", @"dir1\dir2\", "dir2", true)]
    [DataRow(@"dir1/dir2/", @"dir1\dir2\", "dir2")]
    [DataRow(@"/dir1/dir2", @"dir1\dir2\", "dir2", true)]
    [DataRow(@"/dir1/dir2/", @"dir1\dir2\", "dir2")]
    [DataRow(@"dir1\dir2", @"dir1\dir2\", "dir2", true)]
    [DataRow(@"dir1\dir2\", @"dir1\dir2\", "dir2")]
    [DataRow(@"\dir1\dir2", @"dir1\dir2\", "dir2", true)]
    [DataRow(@"\dir1\dir2\", @"dir1\dir2\", "dir2")]
    // middle directory separator chars are consolidated to one:
    [DataRow(@"//dir1//", @"dir1\", "dir1")]
    [DataRow(@"\\dir1\\", @"dir1\", "dir1")]
    [DataRow(@"//dir1/dir2//", @"dir1\dir2\", "dir2")]
    [DataRow(@"\\dir1\dir2\\", @"dir1\dir2\", "dir2")]
    [DataRow(@"//dir1///dir2//", @"dir1\dir2\", "dir2")]
    [DataRow(@"\\dir1\\\dir2\\", @"dir1\dir2\", "dir2")]
    [DataRow(@"\/dir1/\dir2/\", @"dir1\dir2\", "dir2")]
    public void ValidDirectoryPathTest(string fullPath, string expectedRelativePathWindows, string expectedName, bool forceAsDirectory = false)
    {
        // To simplify test case inputs, we expect them to be specified using windows path separators and replace them with the current platform's chars
        var expectedRelativePath = expectedRelativePathWindows.Replace('\\', Path.DirectorySeparatorChar);

        var archivePath = forceAsDirectory ? PaArchivePath.AsDirectoryOrRoot(fullPath) : new PaArchivePath(fullPath);
        archivePath.FullName.Should().Be(expectedRelativePath);
        archivePath.Name.Should().Be(expectedName);
        archivePath.IsDirectory.Should().BeTrue();
    }

    /// <summary>
    /// Zip entry paths are technically allowed to contain any chars.
    /// This can cause security and cross-platform issues when these paths are interpreted by unzipping clients.
    /// e.g. volume separators, relative path (..), wild card chars would need to be protected against by all callers.
    /// For <see cref="PaArchivePath"/>s, we are explicitly dissallowing certain problematic path chars and even segments
    /// which may cause these kinds of problems.
    /// We also exclude other certain symbols as a means of minimiing other potential uses in the future.
    /// e.g. chars that have special meaning in urls, code languages, etc.
    /// </summary>
    [TestMethod]
    // ASCII Control Chars
    [DataRow("ascii-\0-null", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("ascii-\t-tab", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("ascii-\n-newline", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("ascii-\r-carriage-return", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("ascii-\b-backspace", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("ascii-\u001b-esc", PaArchivePathInvalidReason.InvalidPathChars)]
    // Volume separators are potentially malicious
    [DataRow(@"C:\", PaArchivePathInvalidReason.InvalidPathChars)]
    // Windows special path chars:
    [DataRow(@"foo<bar", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow(@"foo>bar", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow(@"foo|>bar", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow(@"foo?bar", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow(@"foo*bar", PaArchivePathInvalidReason.InvalidPathChars)]
    // Quotes (double-quote is still invalid on Windows)
    [DataRow(@"foo""bar", PaArchivePathInvalidReason.InvalidPathChars)]
    // Whitespace in segments:
    [DataRow(@" ", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    [DataRow(@" \dir1\", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    [DataRow(@"dir1\  \dir2\", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    [DataRow(@"dir1\ dir2\", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    [DataRow(@"dir1\dir2 \", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    [DataRow(@"dir1\  dir2  \", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    [DataRow(@"dir1\  ", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    [DataRow(@"dir1\ file3", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    [DataRow(@"dir1\file3 \", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    [DataRow(@"dir1\  file3  ", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    // Relative directory segments are illegal (See: Zip Path Traversal vulnerability)
    [DataRow(@"parent\..\dir", PaArchivePathInvalidReason.RelativeSegment)]
    [DataRow(@"malicious\..\..\..\path", PaArchivePathInvalidReason.RelativeSegment)]
    [DataRow("""..\..\..\Windows\System32\drivers\etc\hosts""", PaArchivePathInvalidReason.RelativeSegment)]
    [DataRow(@"./current/dir", PaArchivePathInvalidReason.RelativeSegment)]
    [DataRow(@"current/./dir", PaArchivePathInvalidReason.RelativeSegment)]
    [DataRow(@"illegal/win/filename.", PaArchivePathInvalidReason.SegmentEndsWithDot)] // Windows: cannot end with a period
    [DataRow(@"illegal/win/segment./filename", PaArchivePathInvalidReason.SegmentEndsWithDot)] // Windows: cannot end with a period
    // extended-length paths are not allowed:
    [DataRow("""\\?\some\long\file\path""", PaArchivePathInvalidReason.InvalidPathChars)]
    public void InvalidPathTest(string fullName, PaArchivePathInvalidReason expectedReason)
    {
        var expectedExMessage = PaArchivePath.GetInvalidReasonExceptionMessage(expectedReason);
        expectedExMessage.Should().NotBeNullOrEmpty("because we should have a message for every invalid reason");

        FluentActions.Invoking(() => new PaArchivePath(fullName))
            .Should().ThrowExactly<ArgumentException>()
#pragma warning disable CA1507 // Use nameof to express symbol names
            .WithParameterName("fullName", "because it's the name of the parameter in the ctor")
#pragma warning restore CA1507 // Use nameof to express symbol names
            .WithMessage(expectedExMessage + "*");
    }

    [TestMethod]
    public void GetInvalidPathCharsTest()
    {
        var invalidChars = PaArchivePath.GetInvalidPathChars();
        invalidChars.Should().NotBeEmpty();
        invalidChars.Should().Contain(Path.GetInvalidPathChars(), "because all chars returned by Path.GetInvalidPathChars for the current platform should be included");
        invalidChars.Should().Contain(Path.GetInvalidFileNameChars().Where(c => !PaArchivePath.GetAllSeparatorChars().Contains(c)), "because all chars returned by Path.GetInvalidFileNameChars (except for separator chars) for the current platform should be included");
        invalidChars.Should().Contain('\b', "BS char should not be allowed");
        invalidChars.Should().Contain('\t', "HT char should not be allowed");
        invalidChars.Should().Contain('\x007F', "DEL char should not be allowed, even though the Path.GetInvalidPathChars doesn't have it");
        PaArchivePath.GetInvalidPathChars().Should().NotBeSameAs(invalidChars, "because we should be returning a new array instance each time to prevent callers from modifying the cached array");
    }

    [TestMethod]
    public void GetAllSeparatorCharsTest()
    {
        var allSeparatorChars = PaArchivePath.GetAllSeparatorChars();
        allSeparatorChars.Should().NotBeEmpty();
        allSeparatorChars.Should().Contain(Path.DirectorySeparatorChar, "because the current platform's directory separator char should be included");
        allSeparatorChars.Should().Contain(Path.AltDirectorySeparatorChar, "because the current platform's alternate directory separator char should be included");
        PaArchivePath.GetAllSeparatorChars().Should().NotBeSameAs(allSeparatorChars, "because we should be returning a new array instance each time to prevent callers from modifying the cached array");
    }

    [TestMethod]
    public void GetInvalidSegmentCharsTest()
    {
        var invalidSegmentChars = PaArchivePath.GetInvalidSegmentChars();
        invalidSegmentChars.Should().NotBeEmpty();
        invalidSegmentChars.Should().Contain(PaArchivePath.GetInvalidPathChars(), "because all chars from GetInvalidPathChars should be included");
        invalidSegmentChars.Should().Contain(PaArchivePath.GetAllSeparatorChars(), "because separator chars delimit segments and are therefore not valid within a segment");
        invalidSegmentChars.Should().Contain(Path.GetInvalidPathChars(), "because all chars returned by Path.GetInvalidPathChars for the current platform should be included");
        invalidSegmentChars.Should().Contain(Path.GetInvalidFileNameChars(), "because all chars returned by Path.GetInvalidFileNameChars for the current platform should be included");
        PaArchivePath.GetInvalidSegmentChars().Should().NotBeSameAs(invalidSegmentChars, "because we should be returning a new array instance each time to prevent callers from modifying the cached array");
    }

    [TestMethod]
    // Valid segments
    [DataRow("file.txt")]
    [DataRow("SomeName")]
    [DataRow(".hidden")]
    [DataRow("some-dir_with555.common.chars")]
    // Whitespace only
    [DataRow("", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    [DataRow(" ", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    [DataRow("   ", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    [DataRow("\t", PaArchivePathInvalidReason.WhitespaceOnlySegment)]
    // Invalid chars (including separator chars)
    [DataRow("foo<bar", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("foo*bar", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("ascii-\0-null", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow("has/separator", PaArchivePathInvalidReason.InvalidPathChars)]
    [DataRow(@"has\separator", PaArchivePathInvalidReason.InvalidPathChars)]
    // Leading/trailing whitespace
    [DataRow(" file", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    [DataRow("file ", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    [DataRow("  file  ", PaArchivePathInvalidReason.SegmentWithLeadingOrTrailingWhitespace)]
    // Illegal segments
    [DataRow("..", PaArchivePathInvalidReason.RelativeSegment)]
    [DataRow(".", PaArchivePathInvalidReason.RelativeSegment)]
    [DataRow("ends-with-dot.", PaArchivePathInvalidReason.SegmentEndsWithDot)]
    public void TryValidateSegmentTest(string segment, PaArchivePathInvalidReason? expectedReason = null)
    {
        var expectIsValid = expectedReason is null;
        PaArchivePath.TryValidateSegment(segment, out var reason)
            .Should().Be(expectIsValid);
        reason.Should().Be(expectedReason);

        PaArchivePath.IsValidSegment(segment)
            .Should().Be(expectIsValid, "because IsValidSegment should agree with TryValidateSegment");
    }

    [TestMethod]
    // Already valid (returned as-is)
    [DataRow("file.txt", "file.txt")]
    [DataRow(".hidden", ".hidden")]
    [DataRow("foo-  -bar.txt", "foo-  -bar.txt")]
    [DataRow("foo-éñü-bar.txt", "foo-éñü-bar.txt")]
    [DataRow("foo-あア-bar.txt", "foo-あア-bar.txt")]
    [DataRow("foo-中文-bar.txt", "foo-中文-bar.txt")]
    // Invalid chars are removed
    [DataRow("foo<bar", "foobar")]
    [DataRow("foo*bar", "foobar")]
    [DataRow("foo\tbar", "foobar")]
    [DataRow("has/separator", "hasseparator")]
    [DataRow(@"has\separator", "hasseparator")]
    // Whitespace trimming
    [DataRow(" file ", "file")]
    [DataRow("  file  ", "file")]
    [DataRow(" \t file \t ", "file")]
    [DataRow(" fi  le ", "fi  le")]
    // Results in empty → false
    [DataRow("", null)]
    [DataRow(" ", null)]
    [DataRow("\t", null)]
    [DataRow("/", null)]
    [DataRow("<>|", null)]
    // Illegal segments
    [DataRow(".", null)]
    [DataRow("..", null)]
    [DataRow("...", null)]
    [DataRow(" . ", null)]
    [DataRow(" .. ", null)]
    [DataRow(" ... ", null)]
    [DataRow("ends-with-dot.", "ends-with-dot")]
    [DataRow("ends-with-dots...", "ends-with-dots")]
    public void TryMakeValidSegmentTest(string segment, string? expectedValidSegment)
    {
        PaArchivePath.TryMakeValidSegment(segment, out var validSegment)
            .Should().Be(expectedValidSegment is not null);
        validSegment.Should().Be(expectedValidSegment);
    }

    [TestMethod]
    // Already valid (returned as-is)
    [DataRow("file.txt", "file.txt")]
    [DataRow(".hidden", ".hidden")]
    [DataRow("foo-  -bar.txt", "foo-  -bar.txt")]
    [DataRow("foo-éñü-bar.txt", "foo-éñü-bar.txt")]
    [DataRow("foo-あア-bar.txt", "foo-あア-bar.txt")]
    [DataRow("foo-中文-bar.txt", "foo-中文-bar.txt")]
    // Invalid chars are replaced
    [DataRow("foo<bar", "foo_bar")]
    [DataRow("foo*bar", "foo_bar")]
    [DataRow("foo\tbar", "foo_bar")]
    [DataRow("has/separator", "has_separator")]
    [DataRow(@"has\separator", "has_separator")]
    // Whitespace replaced (no longer trimmed when a replacement char is supplied)
    [DataRow(" file ", "_file_")]
    [DataRow("  file", "__file")]
    [DataRow("  file  ", "__file__")]
    [DataRow("file  ", "file__")]
    [DataRow(" \t file \t ", "___file___")]
    [DataRow(" fi  le ", "_fi  le_")]
    // Results in empty/replacement-only
    [DataRow("", null)]
    [DataRow(" ", "_")]
    [DataRow("\t", "_")]
    [DataRow("/", "_")]
    [DataRow("<>|", "___")]
    // Illegal segments
    [DataRow(".", "_")]
    [DataRow("..", "._")]
    [DataRow("...", ".._")]
    [DataRow(" . ", "_._")]
    [DataRow(" .. ", "_.._")]
    [DataRow(" ... ", "_..._")]
    [DataRow("ends-with-dot.", "ends-with-dot_")]
    [DataRow("ends-with-dots...", "ends-with-dots.._")]
    public void TryMakeValidSegment_WithReplacementCharTest(string segment, string? expectedValidSegment)
    {
        PaArchivePath.TryMakeValidSegment(segment, out var validSegment, replacementChar: '_')
            .Should().Be(expectedValidSegment is not null);
        validSegment.Should().Be(expectedValidSegment);
    }
    [TestMethod]
    public void GetHashCodeTest()
    {
        // We need to make sure that the hashcode matches the DefaultStringComparer logic, namely, it should be case-insensitive.
        new PaArchivePath(@"dir1\file.txt").GetHashCode().Should().Be(new PaArchivePath(@"DIR1\FILE.TXT").GetHashCode(), "because hash code should be case-insensitive");
    }

    [TestMethod]
    public void ContainsPathTest()
    {
        using var _ = new AssertionScope();

        var root = PaArchivePath.Root;
        root.ContainsPath("file.txt").Should().BeTrue();
        root.ContainsPath("dir1/file.txt").Should().BeTrue();
        root.ContainsPath("dir1/dir2/file.txt").Should().BeTrue();
        root.ContainsPath("dir3/file.txt").Should().BeTrue();

        root.ContainsPath("file.txt", nonRecursive: true).Should().BeTrue();
        root.ContainsPath("dir1/file.txt", nonRecursive: true).Should().BeFalse();
        root.ContainsPath("dir1/dir2/file.txt", nonRecursive: true).Should().BeFalse();
        root.ContainsPath("dir3/file.txt", nonRecursive: true).Should().BeFalse();

        var dir1 = new PaArchivePath(@"dir1/");
        dir1.ContainsPath("file.txt").Should().BeFalse();
        dir1.ContainsPath("dir1/file.txt").Should().BeTrue();
        dir1.ContainsPath("dir1/dir2/file.txt").Should().BeTrue();
        dir1.ContainsPath("dir3/file.txt").Should().BeFalse();

        dir1.ContainsPath("file.txt", nonRecursive: true).Should().BeFalse();
        dir1.ContainsPath("dir1/file.txt", nonRecursive: true).Should().BeTrue();
        dir1.ContainsPath("dir1/dir2/file.txt", nonRecursive: true).Should().BeFalse();
        dir1.ContainsPath("dir3/file.txt", nonRecursive: true).Should().BeFalse();
    }
}
