// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public record ControlInstance
{
    [property: YamlMember(Alias = "Control")]
    public required string ControlType { get; init; }

    public string? Variant { get; init; }

    public string? ComponentName { get; init; }

    public string? ComponentLibraryUniqueName { get; init; }

    public SortedDictionary<string, PFxExpressionYaml>? Properties { get; init; }

    public List<Dictionary<string, ControlInstance>>? Children { get; init; }
}
