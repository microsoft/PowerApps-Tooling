// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record AppInstance
{
    [YamlIgnore]
    public PaYamlLocation? Start { get; init; }

    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    // WorkItem 27966436: Support saving AppHost instances to top-level property 'App'
}
