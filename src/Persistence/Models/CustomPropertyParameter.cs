// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomPropertyParameter
{
    [YamlIgnore]
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DataType DataType { get; init; }
    public bool IsRequired { get; init; }
}
