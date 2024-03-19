// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomProperty
{
    public string Name { get; init; } = string.Empty;

    [YamlMember(Alias = "PropertyKind")]
    public PropertyKind Kind { get; init; } = PropertyKind.Input;

    [YamlMember(Alias = "PropertyType")]
    public PropertyType Type { get; init; } = PropertyType.Data;

    public DataType DataType { get; init; } = DataType.String;

    public string? DisplayName { get; init; }

    public string? Description { get; init; }

    public string? Default { get; init; }

    public string? Tooltip { get; init; }

    public IList<CustomPropertyParameter> Parameters { get; init; } = new List<CustomPropertyParameter>();

    public enum PropertyKind
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
