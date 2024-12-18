// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record ControlInstance : IPaControlInstanceContainer
{
    public ControlInstance() { }

    [SetsRequiredMembers]
    public ControlInstance(string controlType)
    {
        ControlType = controlType ?? throw new ArgumentNullException(nameof(controlType));
    }

    [property: YamlMember(Alias = "Control")]
    public required string ControlType { get; init; }

    public string? Variant { get; init; }

    public string? ComponentName { get; init; }

    public string? ComponentLibraryUniqueName { get; init; }

    public string? Layout { get; init; }

    public bool? Locked { get; init; }

    /// <summary>
    /// The name of the group of controls that this control should be grouped with.
    /// This does not impact the visual layout of the control or behavior, but is used to group controls together for organizational purposes from within the Studio.
    /// </summary>
    [property: YamlMember(Alias = "Group")]
    public string? GroupName { get; init; }

    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    public NamedObjectSequence<ControlInstance>? Children { get; init; }
}
