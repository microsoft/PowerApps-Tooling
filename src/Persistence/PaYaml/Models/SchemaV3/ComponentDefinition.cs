// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

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
    public string? Description { get; init; }

    public bool AccessAppScope { get; init; }

    public NamedObjectMapping<ComponentCustomPropertyUnion>? CustomProperties { get; init; }

    public NamedObjectMapping<PFxExpressionYaml>? Properties { get; init; }

    public NamedObjectSequence<ControlInstance>? Children { get; init; }
}

public abstract record ComponentCustomPropertyBase
{
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    public required ComponentPropertyKind PropertyKind { get; init; }

    public string? DisplayName { get; init; }

    public string? Description { get; init; }
}

/// <summary>
/// Represents the union of all possible properties available on any custom property.
/// </summary>
public record ComponentCustomPropertyUnion : ComponentCustomPropertyBase
{
    public PFxDataType? DataType { get; init; }

    public bool? RaiseOnReset { get; init; }

    public PFxExpressionYaml? Default { get; init; }

    public PFxFunctionReturnType? ReturnType { get; init; }

    public NamedObjectSequence<PFxFunctionParameter> Parameters { get; init; } = new();
}
