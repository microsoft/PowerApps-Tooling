// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Callbacks;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomProperty
{
    [YamlIgnore]
    public required string Name { get; set; }

    public PropertyDirection Direction { get; init; } = PropertyDirection.Input;

    [YamlMember(Alias = "PropertyType")]
    public PropertyType Type { get; init; } = PropertyType.Data;

    public string DataType { get; init; } = "String";

    [YamlIgnore]
    public PropertyCategory Category => Type switch
    {
        PropertyType.Action => PropertyCategory.Behavior,
        PropertyType.Event => PropertyCategory.Behavior,
        PropertyType.Data => PropertyCategory.Data,
        PropertyType.Function => PropertyCategory.Data,
        _ => throw new InvalidOperationException($"Invalid property type: {Type}")
    };

    public bool IsResettable { get; init; }

    public string? DisplayName { get; init; }

    public string? Description { get; init; }

    public string? Default { get; init; }

    public CustomPropertyParametersCollection Parameters { get; init; } = new();

    public enum PropertyDirection
    {
        Input,
        Output
    }

    public enum PropertyType
    {
        Data,
        Event,
        Function,
        Action
    }

    [OnDeserialized]
    internal void AfterDeserialize()
    {
        if (Parameters != null)
        {
            foreach (var kv in Parameters)
            {
                kv.Value.Name = kv.Key;
            }
        }
    }
}
