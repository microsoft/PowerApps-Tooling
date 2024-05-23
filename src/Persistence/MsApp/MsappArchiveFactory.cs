// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

/// <summary>
/// Msapp archive factory.
/// </summary>
public class MsappArchiveFactory(IYamlSerializationFactory yamlSerializationFactory) : IMsappArchiveFactory
{
    private readonly IYamlSerializationFactory _yamlSerializationFactory = yamlSerializationFactory ?? throw new ArgumentNullException(nameof(yamlSerializationFactory));

    public IMsappArchive Create(string path, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        var fileStream = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

        return new MsappArchive(fileStream, ZipArchiveMode.Create, _yamlSerializationFactory);
    }

    public IMsappArchive Create(Stream stream, bool leaveOpen = false)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        return new MsappArchive(stream, ZipArchiveMode.Create, leaveOpen, _yamlSerializationFactory);
    }

    public IMsappArchive Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        return new MsappArchive(path, _yamlSerializationFactory);
    }

    public IMsappArchive Open(Stream stream, bool leaveOpen = false)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        return new MsappArchive(stream, ZipArchiveMode.Read, leaveOpen, _yamlSerializationFactory);
    }

    public IMsappArchive Update(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        return new MsappArchive(fileStream, ZipArchiveMode.Update, leaveOpen: false, _yamlSerializationFactory);
    }

    public IMsappArchive Update(Stream stream, bool leaveOpen = false)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));

        return new MsappArchive(stream, ZipArchiveMode.Update, leaveOpen, _yamlSerializationFactory);
    }
}
