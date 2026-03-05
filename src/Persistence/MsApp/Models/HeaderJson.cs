// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.MsApp.Models;

/// <summary>
/// Model class for header.json file in msapp archive.
/// See same class in DocumentServer.Core for updated schema.
/// </summary>
internal sealed record HeaderJson
{
    /// <summary>
    /// When the header doesn't have a version, this should be the assumed semantic version.
    /// </summary>
    public static readonly Version MSAppV1_0Version = new(1, 0);

    public required Version DocVersion { get; init; }
    public required Version MinVersionToLoad { get; init; }
    public Version? MSAppStructureVersion { get; init; }
    public DateTime? LastSavedDateTimeUTC { get; init; }

    public JsonElement? AnalysisOptions { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
