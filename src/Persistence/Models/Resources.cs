// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public class Resources
{
    [JsonPropertyName("Resources")]
    public IList<Resource> Items { get; set; } = new List<Resource>();
}

public class Resource
{
    public required string Name { get; init; }
    public required string Schema { get; init; }
    public bool IsSampleData { get; set; }
    public bool IsWritable { get; set; }
    public string Type { get; set; } = "ResourceInfo";
    public required string FileName { get; init; }
    public required string Path { get; init; }
    public required string Content { get; init; }
    public string ResourceKind { get; set; } = "LocalFile";
    public string? RootPath { get; set; }
}
