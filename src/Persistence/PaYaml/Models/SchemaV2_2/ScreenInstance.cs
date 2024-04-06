// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public record ScreenInstance
{
    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    public NamedObjectSequence<ControlInstance>? Children { get; init; }
}
