// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

/// <summary>
/// Represents a reusable control style that can be applied to controls of a specific type.
/// </summary>
/// <remarks>
/// A control style is identified by its name, which is unique within the containing <see cref="PaModule.ControlStyles"/> mapping.
/// Each style applies only to the single target control type specified by <see cref="ControlType"/>.
/// </remarks>
public record ControlStyleDefinition
{
    /// <summary>
    /// The target control type that this style applies to (e.g. "Button"). Matches <see cref="ControlInstance.ControlType"/>.
    /// </summary>
    [property: YamlMember(Alias = "Control")]
    public required string? ControlType { get; init; }

    /// <summary>
    /// The set of property formulas defined by this style.
    /// </summary>
    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }
}
