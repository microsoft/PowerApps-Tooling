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
    [DataRow(@"parent\..\dir", PaArchivePathInvalidReason.IllegalSegment)]
    [DataRow(@"malicious\..\..\..\path", PaArchivePathInvalidReason.IllegalSegment)]
    [DataRow("""..\..\..\Windows\System32\drivers\etc\hosts""", PaArchivePathInvalidReason.IllegalSegment)]
    [DataRow(@"./current/dir", PaArchivePathInvalidReason.IllegalSegment)]
    [DataRow(@"current/./dir", PaArchivePathInvalidReason.IllegalSegment)]
    [DataRow(@"illegal/win/filename.", PaArchivePathInvalidReason.IllegalSegment)] // Windows: cannot end with a period
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
