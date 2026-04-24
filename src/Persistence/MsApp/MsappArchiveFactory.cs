// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Msapp archive factory.
/// </summary>
public class MsappArchiveFactory(ILogger<MsappArchive>? _logger = null) : IMsappArchiveFactory
{
    /// <summary>
    /// Instance of <see cref="MsappArchiveFactory"/> where no logger is available.
    /// Helps with using in tests where logging is not needed.
    /// </summary>
    public static readonly MsappArchiveFactory Default = new();

    public MsappArchive Create(string path, bool overwrite = false)
    {
        ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        return Create(fileStream, leaveOpen: false);
    }

    public MsappArchive Create(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Create, leaveOpen, _logger);
    }

    public MsappArchive Open(string path)
    {
        ThrowIfNullOrWhiteSpace(path);

        var fileStream = File.OpenRead(path);

        return Open(fileStream, leaveOpen: false);
    }

    public MsappArchive Open(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Read, leaveOpen, _logger);
    }

    public MsappArchive Update(string path)
    {
        ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        return Update(fileStream, leaveOpen: false);
    }

    public MsappArchive Update(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Update, leaveOpen, _logger);
    }
}
