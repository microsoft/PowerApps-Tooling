// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3_0;

public record ControlInstance
{
    public ControlInstance() { }

    [SetsRequiredMembers]
    public ControlInstance(string controlTypeId)
    {
        ControlTypeId = controlTypeId ?? throw new ArgumentNullException(nameof(controlTypeId));
    }

    [property: YamlMember(Alias = "Control")]
    public required string ControlTypeId { get; init; }

    public string? Variant { get; init; }

    public string? ComponentName { get; init; }

    public string? ComponentLibraryUniqueName { get; init; }

    public NamedObjectMapping<PFxExpressionYaml> Properties { get; init; } = new();

    public NamedObjectMapping<ControlGroup> Groups { get; init; } = new();

    public NamedObjectSequence<ControlInstance> Children { get; init; } = new();
}
