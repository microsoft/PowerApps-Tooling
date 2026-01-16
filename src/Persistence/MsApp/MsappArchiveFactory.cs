// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;

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

    public Encoding? EntryNameEncoding { get; init; }

    public IMsappArchive Create(string path, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        var fileStream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        return Create(fileStream, leaveOpen: false);
    }

    public IMsappArchive Create(Stream stream, bool leaveOpen = false)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        return new MsappArchive(stream, ZipArchiveMode.Create, leaveOpen, EntryNameEncoding);
    }

    public IMsappArchive Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Open(fileStream, leaveOpen: false);
    }

    public IMsappArchive Open(Stream stream, bool leaveOpen = false)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        return new MsappArchive(stream, ZipArchiveMode.Read, leaveOpen, EntryNameEncoding);
    }

    public IMsappArchive Update(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        return Update(fileStream, leaveOpen: false);
    }

    public IMsappArchive Update(Stream stream, bool leaveOpen = false)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        return new MsappArchive(stream, ZipArchiveMode.Update, leaveOpen, EntryNameEncoding);
    }
}
