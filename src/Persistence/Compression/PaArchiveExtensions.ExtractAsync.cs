// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

public static partial class PaArchiveExtensions
{
#if NET10_0_OR_GREATER
    public static async Task ExtractToFileAsync(this PaArchiveEntry source, string destinationFileName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        // .net 10 supports ExtractToFileAsync
        await source.ZipEntry.ExtractToFileAsync(destinationFileName, overwrite, cancellationToken).ConfigureAwait(false);
    }
#else
    public static ValueTask ExtractToFileAsync(this PaArchiveEntry source, string destinationFileName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        source.ZipEntry.ExtractToFile(destinationFileName, overwrite);
        return ValueTask.CompletedTask;
    }
#endif

    /// <summary>
    /// Extracts the archive to a target directory, preserving relative paths.
    /// </summary>
    /// <remarks>
    /// This method is protected against ZipSlip attacks.
    /// </remarks>
    public static async Task ExtractToDirectoryAsync(this PaArchive source, string destinationDirectoryName, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(source);

        await source.Entries.ExtractToDirectoryAsync(destinationDirectoryName, overwrite).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts the selected archive entries to a target directory, preserving relative paths.
    /// </summary>
    /// <remarks>
    /// This method is protected against ZipSlip attacks.
    /// </remarks>
    public static async Task ExtractToDirectoryAsync(
        this IEnumerable<PaArchiveEntry> entries,
        string destinationDirectoryName,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(destinationDirectoryName);

        foreach (var entry in entries)
        {
            await entry.ExtractRelativeToDirectoryAsync(destinationDirectoryName, overwrite, cancellationToken).ConfigureAwait(false);
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
    public static async Task ExtractRelativeToDirectoryAsync(
        this PaArchiveEntry source,
        string destinationDirectoryName,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinationDirectoryName);

        var fileDestinationPath = ComputeAndValidateExtractToPathRelativeToDirectory(source, destinationDirectoryName);

        // PaArchiveEntry's should always only ever represent a file
        Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
        await source.ExtractToFileAsync(fileDestinationPath, overwrite: overwrite, cancellationToken).ConfigureAwait(false);
    }
}
