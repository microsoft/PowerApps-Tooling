// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record DataSourceInstance
{
    public required string Type { get; init; }
    public string? TableLogicalName { get; init; }
}