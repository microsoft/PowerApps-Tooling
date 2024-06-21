// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record AppInstance : IMayHavePaYamlLocation, ISetPaYamlNodeLocation
{
    [YamlIgnore]
    public PaYamlLocation? Start { get; private set; }

    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    void ISetPaYamlNodeLocation.SetNodeLocation(PaYamlLocation? start)
    {
        Start = start;
    }

    // WorkItem 27966436: Support saving AppHost instances to top-level property 'App'
}
