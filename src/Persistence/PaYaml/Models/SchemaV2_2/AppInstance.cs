// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public record AppInstance
{
    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    public AppInstanceChildren? Children { get; init; }
}

public record AppInstanceChildren
{
    public HostControlInstance? Host { get; init; }

    [YamlIgnore]
    public int Count => Host != null ? 1 : 0;
}

public record HostControlInstance
{
    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }
}
