// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public class DataSources
{
    [JsonPropertyName("DataSources")]
    public List<DataSource>? Items { get; set; }
}

public class DataSource
{
    public string Type { get; set; } = "ServiceInfo";
    public required string Name { get; set; }
    public required string ServiceKind { get; set; }
    public WadlMetadata? WadlMetadata { get; set; }
    public required string ApiId { get; set; }
}

public class WadlMetadata
{
    public required string WadlXml { get; set; }
}
