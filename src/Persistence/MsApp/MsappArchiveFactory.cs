// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

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

    /// <summary>
    /// Optional limits checked when existing archives are opened by <see cref="Open(Stream, bool)"/> or <see cref="Update(Stream, bool)"/>
    /// (and their path overloads). See <see cref="PaArchive(Stream, ZipArchiveMode, PaArchiveLimits?, bool, Encoding?, ILogger{PaArchive}?)"/>.
    /// Null (the default) opens archives without limits.
    /// </summary>
    /// <example><c>new MsappArchiveFactory(logger) { Limits = PaArchiveLimits.Default }</c></example>
    public PaArchiveLimits? Limits { get; init; }

    public MsappArchive Create(string path, bool overwrite = false)
    {
        ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        return Create(fileStream, leaveOpen: false);
    }

    public MsappArchive Create(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Create, Limits, leaveOpen, _logger);
    }

    public MsappArchive Open(string path)
    {
        ThrowIfNullOrWhiteSpace(path);

        var fileStream = File.OpenRead(path);

        return Open(fileStream, leaveOpen: false);
    }

    public MsappArchive Open(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Read, Limits, leaveOpen, _logger);
    }

    public MsappArchive Update(string path)
    {
        ThrowIfNullOrWhiteSpace(path);

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        return Update(fileStream, leaveOpen: false);
    }

    public MsappArchive Update(Stream stream, bool leaveOpen = false)
    {
        return new MsappArchive(stream, ZipArchiveMode.Update, Limits, leaveOpen, _logger);
    }
}
