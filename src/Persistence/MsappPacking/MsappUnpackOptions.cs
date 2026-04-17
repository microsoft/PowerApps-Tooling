// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Options for <see cref="MsappPackingService.UnpackToDirectoryAsync"/>.
/// </summary>
public sealed record MsappUnpackOptions
{
    /// <summary>
    /// Indicates whether to allow overwriting output files/folders if they already exist.
    /// </summary>
    public bool OverwriteOutput { get; init; }

    /// <summary>
    /// Configuration describing which content types to unpack.
    /// If null, the default <see cref="UnpackedConfiguration"/> is used.
    /// </summary>
    public UnpackedConfiguration UnpackedConfig { get; init; } = new();

    /// <summary>
    /// The name (without extension) to use for the output .msapr file.
    /// If null, defaults to the file name without extension of the source .msapp file.
    /// </summary>
    public string? MsaprName { get; init; }
}
