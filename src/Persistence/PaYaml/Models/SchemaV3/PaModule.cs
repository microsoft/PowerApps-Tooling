// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

/// <summary>
/// Represents a Power Apps Yaml module file.
/// </summary>
public record PaModule
{
    public AppInstance? App { get; init; }
    public NamedObjectMapping<ComponentDefinition> ComponentDefinitions { get; init; } = new();
    public NamedObjectMapping<ScreenInstance> Screens { get; init; } = new();
}
