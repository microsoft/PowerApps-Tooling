// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Compression;

/// <summary>
/// Identifies a limit in <see cref="PaArchiveLimits"/>.
/// </summary>
public enum PaArchiveLimit
{
    /// <summary>
    /// No limit was exceeded.
    /// </summary>
    None = 0,

    /// <summary>
    /// <see cref="PaArchiveLimits.MaxEntryCount"/>
    /// </summary>
    EntryCount,

    /// <summary>
    /// <see cref="PaArchiveLimits.MaxCompressedSize"/>
    /// </summary>
    CompressedSize,

    /// <summary>
    /// <see cref="PaArchiveLimits.MaxExpandedSize"/>
    /// </summary>
    ExpandedSize,

    /// <summary>
    /// <see cref="PaArchiveLimits.MaxEntrySize"/>
    /// </summary>
    EntrySize,

    /// <summary>
    /// <see cref="PaArchiveLimits.MaxTextEntrySize"/>
    /// </summary>
    TextEntrySize,

    /// <summary>
    /// <see cref="PaArchiveLimits.MaxCompressionRatio"/>
    /// </summary>
    CompressionRatio,
}
