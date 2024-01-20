// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;

namespace Microsoft.PowerPlatform.Formulas.Tools.MsApp;

/// <summary>
/// base interface for MsappArchive
/// </summary>
public interface IMsappArchive
{
    /// <summary>
    /// Creates a new entry in the archive with the given name.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    ZipArchiveEntry CreateEntry(string entryName);

    /// <summary>
    /// Returns the entry in the archive with the given name.
    /// </summary>
    /// <param name="entryName"></param>
    /// <returns>the entry or null when not found.</returns>
    ZipArchiveEntry GetEntry(string entryName);

    /// <summary>
    /// Provides access to the underlying zip archive.
    /// </summary>
    ZipArchive ZipArchive { get; }
}
