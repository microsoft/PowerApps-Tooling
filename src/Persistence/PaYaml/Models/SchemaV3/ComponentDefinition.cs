// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public enum ComponentDefinitionType
{
    CanvasComponent,
    CommandComponent,
}

public enum ComponentPropertyKind
{
    Input,
    Output,
    InputFunction,
    OutputFunction,
    Event,
    Action,
}

public record ComponentDefinition : IPaControlInstanceContainer
{
    // WARNING: this property is required, but yamlDotNet doesn't enforce required properties correctly.
    // The validation here must be done by the compiler, otherwise we'll need to add a custom validator.
    //[YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    //public required ComponentDefinitionType DefinitionType { get; init; }
    public required ComponentDefinitionType? DefinitionType { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// Only applicable for components in Component Libraries.
    /// </summary>
    public bool? AllowCustomization { get; init; }

    /// <summary>
    /// Only applicable for <see cref="ComponentDefinitionType.CanvasComponent"/> that are NOT in a Component Library.
    /// </summary>
    public bool? AccessAppScope { get; init; }

    /// <summary>
    /// Only applicable for <see cref="ComponentDefinitionType.CanvasComponent"/>.
    /// </summary>
    public NamedObjectMapping<ComponentCustomPropertyUnion>? CustomProperties { get; init; }

    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    /// <summary>
    /// Only applicable for <see cref="ComponentDefinitionType.CanvasComponent"/>.
    /// </summary>
    public NamedObjectSequence<ControlInstance>? Children { get; init; }
}

public abstract record ComponentCustomPropertyBase
{
    [YamlMember(Order = -9, DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    public required ComponentPropertyKind PropertyKind { get; init; }

    [YamlMember(Order = -8)]
    public string? DisplayName { get; init; }

    [YamlMember(Order = -7)]
    public string? Description { get; init; }
}

/// <summary>
/// Represents the union of all possible properties available on any custom property.
/// </summary>
public record ComponentCustomPropertyUnion : ComponentCustomPropertyBase
{
    public bool? RaiseOnReset { get; init; }

    public PaYamlPropertyDataType? DataType { get; init; }

    public PaYamlPropertyDataType? ReturnType { get; init; }

    /// <summary>
    /// The default script for this custom input property.
    /// </summary>
    public PFxExpressionYaml? Default { get; init; }

    public NamedObjectSequence<ComponentCustomPropertyParameter>? Parameters { get; init; }
}

public record ComponentCustomPropertyParameter()
{
    public string? Description { get; init; }

    public bool IsOptional { get; init; }

    public PaYamlPropertyDataType? DataType { get; init; }

    /// <summary>
    /// The default script for this optional parameter.
    /// </summary>
    public PFxExpressionYaml? Default { get; init; }
}
