// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// base interface for MsappArchive
/// </summary>
public interface IMsappArchive : IPaArchive, IDisposable
{
    Version MSAppStructureVersion { get; }

    Version DocVersion { get; }

    /// <summary>
    /// Provides access to the underlying zip archive.
    /// Attention: This property might be removed in the future.
    /// </summary>
    [Obsolete("We shouldn't expose the underlying ZipArchive instance, as modifying it will make this instance inconsistent")]
    ZipArchive ZipArchive { get; }

    /// <summary>
    /// Attempts to generate a unique entry path in the specified directory, based on a starter file name and extension.
    /// </summary>
    /// <param name="directory">The directory path for the entry; or null. Note: Directory path is expected to already be safe, as it is expected to not contain customer data.</param>
    /// <param name="fileNameNoExtension">The name of the file, without an extension. This file name should already have been made 'safe' by the caller. If needed, use <see cref="MsappArchive.TryMakeSafeForEntryPathSegment"/> to make it safe.</param>
    /// <param name="extension">The file extension to add for the file path or null for no extension. Note: This should already contain only safe chars, as it is expected to not contain customer data.</param>
    /// <param name="uniqueSuffixSeparator">The string added just before the unique file number and extension. Default is empty string.</param>
    /// <returns>The entry path which is unique in the specified <paramref name="directory"/>.</returns>
    /// <exception cref="ArgumentException">directory is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">A unique filename entry could not be generated.</exception>
    string GenerateUniqueEntryPath(
        string? directory,
        string fileNameNoExtension,
        string? extension,
        string uniqueSuffixSeparator = "");
}
