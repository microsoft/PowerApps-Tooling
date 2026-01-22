// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Msapp archive factory.
/// </summary>
public class MsappArchiveFactory : IMsappArchiveFactory
{
    /// <summary>
    /// Instance of MsappArchiveFactory where <see cref="EntryNameEncoding"/> is `null`.
    /// Helps with using in tests where logging is not needed.
    /// </summary>
    public static readonly MsappArchiveFactory Default = new();

    private readonly ILogger<MsappArchive>? _logger;

    public MsappArchiveFactory(ILogger<MsappArchive>? logger = null)
    {
        _logger = logger;
    }

    public Encoding? EntryNameEncoding { get; init; }

    public IMsappArchive Create(string path, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        return Create(fileStream, leaveOpen: false);
    }

    public IMsappArchive Create(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Create, leaveOpen, EntryNameEncoding, _logger);
    }

    public IMsappArchive Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Open(fileStream, leaveOpen: false);
    }

    public IMsappArchive Open(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Read, leaveOpen, EntryNameEncoding, _logger);
    }

    public IMsappArchive Update(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        return Update(fileStream, leaveOpen: false);
    }

    public IMsappArchive Update(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Update, leaveOpen, EntryNameEncoding, _logger);
    }
}
