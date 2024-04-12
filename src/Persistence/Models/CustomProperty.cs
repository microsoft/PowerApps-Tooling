// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomProperty : INamedObject
{
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

    public IList<CustomPropertyParameter> Parameters { get; set; } = new List<CustomPropertyParameter>();

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
}
