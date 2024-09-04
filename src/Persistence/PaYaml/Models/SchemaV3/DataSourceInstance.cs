// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public enum DataSourceInstanceType
{
    DataverseTable,
}

public record DataSourceInstance
{
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    public required DataSourceInstanceType Type { get; init; }
    public string? TableLogicalName { get; init; }
}
