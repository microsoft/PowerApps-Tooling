// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;

/// <summary>
/// Model class for packed.json file in msapp archives which have been packed via supported tooling.
/// </summary>
public sealed record PackedJson
{
    /// <summary>
    /// The version of the schema supported by this model class.
    /// This does not necessarily mean it will be supported by a given instance of the document server.
    /// </summary>
    public static readonly Version CurrentPackedStructureVersion = new(0, 1);

    public required Version PackedStructureVersion { get; init; }

    public DateTime? LastPackedDateTimeUtc { get; init; }

    public PackedJsonPackingClient? PackingClient { get; init; }

    /// <summary>
    /// Configuration options for how the msapp should be loaded.
    /// </summary>
    public required PackedJsonLoadConfiguration LoadConfiguration { get; init; }
}

public record PackedJsonLoadConfiguration
{
    /// <summary>
    /// Indicates whether this app should be loaded from the *.pa.yaml files in the 'Src' folder of the msapp archive.
    /// </summary>
    public required bool LoadFromYaml { get; init; }
}

/// <summary>
/// Information about the client that performed the packing operation.
/// This is mainly useful for telemetry and support scenarios.
/// </summary>
public record PackedJsonPackingClient
{
    public required string Name { get; init; }

    public string? Version { get; init; }

    /// <summary>
    /// Allow clients to include additional arbitrary information about themselves which may be useful for telemetry or support.
    /// </summary>
    [JsonExtensionData]
    public ImmutableDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
