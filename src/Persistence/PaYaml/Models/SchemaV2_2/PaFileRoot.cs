// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public record PaFileRoot
{
    public AppInstance? App { get; init; }
    public Dictionary<string, ScreenInstance>? Screens { get; init; }
    public Dictionary<string, ComponentDefinition>? ComponentDefinitions { get; init; }
}
