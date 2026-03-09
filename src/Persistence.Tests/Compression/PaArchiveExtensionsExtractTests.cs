// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

namespace Persistence.Tests.Compression;

[TestClass]
public class PaArchiveExtensionsExtractTests : TestBase
{
    [TestMethod]
    [DataRow("file.txt")]
    [DataRow("subdir/file.txt")]
    [DataRow("a/b/c/file.txt")]
    public void TryComputeAndValidateExtractToPath_WithValidPath_ReturnsTrue(string entryPathStr)
    {
        var destDir = CreateTestOutputFolder();
        var entryPath = new PaArchivePath(entryPathStr);

        PaArchiveExtensions.TryComputeAndValidateExtractToPathRelativeToDirectory(destDir, entryPath, out var validFullPath)
            .Should().BeTrue();
        validFullPath.Should().NotBeNull();
        validFullPath.Should().StartWith(Path.GetFullPath(destDir) + Path.DirectorySeparatorChar);
        validFullPath.Should().EndWithEquivalentOf(entryPath.FullName);
    }

    [TestMethod]
    [DataRow(@"foo\..\..\bar\file.txt")]
    public void TryComputeAndValidateExtractToPath_WithPathTraversal_ReturnsFalse(string inputEntryPath)
    {
        // This simulates an entry path that bypassed PaArchivePath validation (e.g. via a crafted zip),
        // resulting in a path that traverses outside the destination directory.
#pragma warning disable CS0618 // TestOnly_CreateInvalidPath is intentionally obsolete for test use only
        var maliciousPath = PaArchivePath.TestOnly_CreateInvalidPath(inputEntryPath);
#pragma warning restore CS0618

        var destDir = CreateTestOutputFolder();

        PaArchiveExtensions.TryComputeAndValidateExtractToPathRelativeToDirectory(destDir, maliciousPath, out var validFullPath)
            .Should().BeFalse();
        validFullPath.Should().BeNull();
    }

    [TestMethod]
    [DataRow(@"foo\..\..\bar\file.txt")]
    public void ComputeAndValidateExtractToPath_WithPathTraversal_ThrowsAndLogsError(string inputEntryPath)
    {
        // Arrange: create a PaArchive with a logger and an entry whose NormalizedPath bypasses
        // PaArchivePath validation, simulating a crafted zip that slipped through.
        using var stream = new MemoryStream();
        var capturingLogger = new CapturingLogger<PaArchive>();
        using var paArchive = new PaArchive(stream, ZipArchiveMode.Create, leaveOpen: true, logger: capturingLogger);

#pragma warning disable CS0618 // TestOnly methods are intentionally obsolete for test use only
        var maliciousPath = PaArchivePath.TestOnly_CreateInvalidPath(inputEntryPath);
#pragma warning restore CS0618

        // Create foux zip entry archive for the bad entry
        var zipEntry = paArchive.InnerZipArchive.CreateEntry(inputEntryPath);
        var entry = new PaArchiveEntry(paArchive, zipEntry, maliciousPath, skipValidation: true);

        var destDir = CreateTestOutputFolder();

        // Act & Assert: extracting should throw IOException
        FluentActions.Invoking(() => PaArchiveExtensions.ComputeAndValidateExtractToPathRelativeToDirectory(entry, destDir))
            .Should().Throw<IOException>()
            .WithMessage($"Extracting {nameof(PaArchiveEntry)} would have resulted in a file outside the specified destination directory.");

        // Assert: the log entry was recorded with Error level
        capturingLogger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
            .Which.Message.Should().Match($"Extracting {nameof(PaArchiveEntry)} would have resulted in a file outside the specified destination directory.*'{inputEntryPath}'*");
    }
}
