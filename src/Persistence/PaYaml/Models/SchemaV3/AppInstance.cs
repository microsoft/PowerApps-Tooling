// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record AppInstance
{
    public NamedObjectMapping<PFxExpressionYaml> Properties { get; init; } = new();

    // WorkItem 27966436: Support saving AppHost instances to top-level property 'App'
}
