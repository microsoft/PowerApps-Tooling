// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public enum DataSourceType
{
    Table,
    Actions
}
public record DataSourceInstance
{
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    public required DataSourceType? Type { get; init; }
    public string? ConnectorId { get; init; }
    public NamedObjectMapping<string>? Parameters { get; init; }
}
