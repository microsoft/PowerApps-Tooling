// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.PowerApps.Persistence.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Represents a .msapr file. aka 'msapp reference' file.
/// This file represents the binary portion of an unpacked msapp.
/// </summary>
public partial class MsappReferenceArchive : PaArchive
{
    private MsaprHeaderJson? _header;

    /// <param name="leaveOpen">true to leave the stream open after the System.IO.Compression.ZipArchive object is disposed; otherwise, false</param>
    internal MsappReferenceArchive(
        Stream stream,
        ZipArchiveMode mode,
        bool leaveOpen = false,
        ILogger<MsappReferenceArchive>? logger = null) : base(stream, mode, leaveOpen, entryNameEncoding: null, logger)
    {
    }

    internal MsaprHeaderJson Header => _header ??= LoadHeader();

    private MsaprHeaderJson LoadHeader()
    {
        return GetRequiredEntry(MsaprLayoutConstants.FileNames.MsaprHeader)
            .DeserializeAsJson<MsaprHeaderJson>(MsaprSerialization.DefaultJsonSerializeOptions);
    }
}
