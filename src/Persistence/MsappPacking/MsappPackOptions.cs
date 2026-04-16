// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking;

/// <summary>
/// Options for <see cref="MsappPackingService.PackFromMsappReferenceFileAsync"/>.
/// </summary>
public sealed record MsappPackOptions
{
    /// <summary>
    /// Indicates whether to allow overwriting the output .msapp file if it already exists.
    /// </summary>
    public bool OverwriteOutput { get; init; }

    /// <summary>
    /// When true, instructs the Power Apps runtime to load from the unpacked YAML source files.
    /// Only valid when <see cref="MsappUnpackableContentType.PaYamlSourceCode"/> was unpacked.
    /// </summary>
    public bool EnableLoadFromYaml { get; init; }
}
