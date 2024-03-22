// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomProperty
{
    public required string Name { get; init; }

    public PropertyDirection Direction { get; init; } = PropertyDirection.Input;

    [YamlMember(Alias = "PropertyType")]
    public PropertyType Type { get; init; } = PropertyType.Data;

    public string DataType { get; init; } = "String";

    public PropertyCategory Category { get; init; } = PropertyCategory.Data;

    public bool IsResettable { get; init; }

    public string? DisplayName { get; init; }

    public string? Description { get; init; }

    public string? Default { get; init; }

    public string? Tooltip { get; init; }

    public IList<CustomPropertyParameter> Parameters { get; init; } = new List<CustomPropertyParameter>();

    public enum PropertyDirection
    {
        Input,
        Output
    }

    public enum PropertyCategory
    {
        Data,
        Behavior
    }

    public enum PropertyType
    {
        Data,
        Event,
        Function,
        Action
    }
}
