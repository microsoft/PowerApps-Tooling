// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsappPacking.Models;

/// <summary>
/// Contents of the <c>msapr-header.json</c> entry written into the .msapr file.
/// </summary>
public sealed record MsaprHeaderJson
{
    public static readonly Version CurrentMsaprStructureVersion = new(0, 1);

    public required Version MsaprStructureVersion { get; init; }

    /// <summary>
    /// Configuration options that were used when the original msapp was unpacked.
    /// </summary>
    public required MsaprHeaderJsonUnpackedConfiguration UnpackedConfiguration { get; init; }

    /// <summary>
    /// In order to support forward-compatible deserialization, we alllow arbitrary additional properties.
    /// </summary>
    [JsonExtensionData]
    public ImmutableDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record MsaprHeaderJsonUnpackedConfiguration
{
    /// <summary>
    /// The types of content in the msapp which were previously unpacked.
    /// </summary>
    public required ImmutableArray<string> ContentTypes { get; init; }

    /// <summary>
    /// In order to support forward-compatible deserialization, we alllow arbitrary additional properties.
    /// </summary>
    [JsonExtensionData]
    public ImmutableDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
