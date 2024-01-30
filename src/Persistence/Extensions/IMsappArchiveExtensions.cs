// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class IMsappArchiveExtensions
{
    /// <summary>
    /// Returns the entry in the archive with the given name or throws a <see cref="FileNotFoundException"/> if it does not exist.
    /// </summary>
    /// <param name="archive">the <see cref="IMsappArchive"/> instance.</param>
    /// <param name="entryName">the name of the entry to fetch.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static ZipArchiveEntry GetRequiredEntry(this IMsappArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName) ??
            throw new FileNotFoundException($"Entry '{entryName}' not found in msapp archive.");

        return entry;
    }
}

