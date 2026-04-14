// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Serialization;
using System.IO.Compression;
using System.Text;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Msapp archive factory.
/// </summary>
public class MsappReferenceArchiveFactory(ILogger<MsappReferenceArchive>? _logger = null)
{
    /// <summary>
    /// Instance of <see cref="MsappReferenceArchiveFactory"/> where no logger is available.
    /// Helps with using in tests where logging is not needed.
    /// </summary>
    public static readonly MsappReferenceArchiveFactory Default = new();

    internal async Task<MsappReferenceArchive> CreateNewAsync(string path, MsaprHeaderJson headerJson, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        return await CreateNewAsync(fileStream, headerJson, leaveOpen: false, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<MsappReferenceArchive> CreateNewAsync(Stream stream, MsaprHeaderJson headerJson, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        var msapr = new MsappReferenceArchive(stream, ZipArchiveMode.Create, leaveOpen, _logger);

        // The first thing that must exist in an msapp-ref file is the header; just like with an msapp
        await msapr.AddEntryFromJsonAsync(MsaprLayoutConstants.FileNames.MsaprHeader, headerJson, MsaprSerialization.DefaultJsonSerializeOptions, cancellationToken).ConfigureAwait(false);

        return msapr;
    }

    public MsappReferenceArchive Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileStream = File.OpenRead(path);

        return Open(fileStream, leaveOpen: false);
    }

    public MsappReferenceArchive Open(Stream stream, bool leaveOpen = false)
    {
        return new MsappReferenceArchive(stream, ZipArchiveMode.Read, leaveOpen, _logger);
    }

    public MsappReferenceArchive Update(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        return Update(fileStream, leaveOpen: false);
    }

    public MsappReferenceArchive Update(Stream stream, bool leaveOpen = false)
    {
        return new MsappReferenceArchive(stream, ZipArchiveMode.Update, leaveOpen, _logger);
    }
}
