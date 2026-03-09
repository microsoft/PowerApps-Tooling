// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

public static partial class PaArchiveExtensions
{
    public static void ExtractToFile(this PaArchiveEntry source, string destinationFileName, bool overwrite = false)
    {
        source.ZipEntry.ExtractToFile(destinationFileName, overwrite);
    }

    /// <summary>
    /// Extracts the archive to a target directory, preserving relative paths.
    /// </summary>
    /// <remarks>
    /// This method is protected against ZipSlip attacks.
    /// </remarks>
    public static void ExtractToDirectory(this PaArchive source, string destinationDirectoryName, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Entries.ExtractToDirectory(destinationDirectoryName, overwrite);
    }

    /// <summary>
    /// Extracts the selected archive entries to a target directory, preserving relative paths.
    /// </summary>
    /// <remarks>
    /// This method is protected against ZipSlip attacks.
    /// </remarks>
    public static void ExtractToDirectory(this IEnumerable<PaArchiveEntry> entries, string destinationDirectoryName, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(destinationDirectoryName);

        foreach (var entry in entries)
        {
            entry.ExtractRelativeToDirectory(destinationDirectoryName, overwrite);
        }
    }

    /// <summary>
    /// Extracts the current entry to the target <paramref name="destinationDirectoryName"/> using the relative entry path as subdirectory./// <br/>
    /// </summary>
    /// <param name="destinationDirectoryName">The target destination directory path. This directory does not need to exist; as it will be created.</param>
    /// <exception cref="IOException">The effective extracted path occurs outside the <paramref name="destinationDirectoryName"/> directory.</exception>
    /// <remarks>
    /// This method is protected against ZipSlip attacks.
    /// </remarks>
    public static void ExtractRelativeToDirectory(this PaArchiveEntry source, string destinationDirectoryName, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinationDirectoryName);

        var fileDestinationPath = ComputeAndValidateExtractToPathRelativeToDirectory(source, destinationDirectoryName);

        // PaArchiveEntry's should always only ever represent a file
        Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
        source.ExtractToFile(fileDestinationPath, overwrite: overwrite);
    }

    /// <summary>
    /// Reusable utility for computing a final unzipped path is in the target directory; preventing ZipSlip attacks.
    /// </summary>
    /// <param name="destinationDirectoryName">The target directory into which the entry should be extracted. This directory does not need to exist, but will at the end of this call.</param>
    public static string ComputeAndValidateExtractToPathRelativeToDirectory(PaArchiveEntry source, string destinationDirectoryName)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinationDirectoryName);

        if (!TryComputeAndValidateExtractToPathRelativeToDirectory(destinationDirectoryName, source.NormalizedPath, out var validFullPath))
        {
            // Log so we can query and fix PaArchivePath to detect these malicious paths
            source.PaArchive.InnerLogger?.LogExtractingResultsInOutside(source.OriginalFullName, source.NormalizedPath);
            throw new IOException($"Extracting {nameof(PaArchiveEntry)} would have resulted in a file outside the specified destination directory.");
        }
        else
        {
            return validFullPath;
        }
    }

    /// <summary>
    /// Reusable utility for computing a final unzipped path is in the target directory; preventing ZipSlip attacks.
    /// </summary>
    /// <param name="destinationDirectoryName">
    /// The target directory into which the entry should be extracted.
    /// If a relative path, it will be relative to the current working directory.
    /// This directory does not need to exist, but will at the end of this call.
    /// </param>
    /// <param name="validFullPath">
    /// When this method returns false, this value is null.
    /// When this method returns true, this parameter has the value of the full path to the computed extraction file path.
    /// </param>
    /// <returns>true if the computed path is valid to extract (and is contained in the target directory). otherwise, false, indicating the computed full path is invalid.</returns>
    public static bool TryComputeAndValidateExtractToPathRelativeToDirectory(string destinationDirectoryName, PaArchivePath entryPath, [NotNullWhen(true)] out string? validFullPath)
    {
        ArgumentNullException.ThrowIfNull(destinationDirectoryName);
        ArgumentNullException.ThrowIfNull(entryPath);

        // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
        var di = Directory.CreateDirectory(destinationDirectoryName);
        var destinationDirectoryFullPath = di.FullName;
        if (!destinationDirectoryFullPath.EndsWith(Path.DirectorySeparatorChar))
        {
            destinationDirectoryFullPath = $"{destinationDirectoryFullPath}{Path.DirectorySeparatorChar}";
        }

        // Note: PaArchiveEntry.FullName has already been sanitized of invalid chars (or all known platforms) due to PaArchivePath
        validFullPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, entryPath.FullName));

        // Catch ZipSlip:
        // Note: PaArchive instances attempt to detect malicious entry paths via the validation in PaArchivePath.
        // But it's possible that certain OS implementations and carefully crafted relative paths may have been missed.
        // This check here ensures we catch any missed conditions by adding the recommended check for a ZipSlip condition.
        if (!validFullPath.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
        {
            validFullPath = null;
            return false;
        }

        return true;
    }
}
