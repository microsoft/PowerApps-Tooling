// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public record ScreenInstance : IPaControlInstanceContainer
{
    public NamedObjectMapping<PFxExpressionYaml> Properties { get; init; } = new();

    public NamedObjectMapping<ControlGroup> Groups { get; init; } = new();

    public NamedObjectSequence<ControlInstance> Children { get; init; } = new();
}
