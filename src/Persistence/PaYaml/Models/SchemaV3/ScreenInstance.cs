// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record ScreenInstance : IPaControlInstanceContainer
{
    public string? Variant { get; init; }

    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    public NamedObjectMapping<ControlGroup>? Groups { get; init; }

    public NamedObjectSequence<ControlInstance>? Children { get; init; }
}
