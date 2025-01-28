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

    public bool AccessAppScope { get; init; }

    public NamedObjectMapping<ComponentCustomPropertyUnion>? CustomProperties { get; init; }

    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

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

    public PFxDataType? DataType { get; init; }

    public PFxFunctionReturnType? ReturnType { get; init; }

    public NamedObjectSequence<PFxFunctionParameter> Parameters { get; init; } = new();

    /// <summary>
    /// The default script for this custom input property.
    /// </summary>
    public PFxExpressionYaml? Default { get; init; }
}
