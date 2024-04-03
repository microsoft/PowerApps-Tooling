// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomPropertyParameter
{
    [YamlIgnore]
    public required string Name { get; set; } = string.Empty;
    public required string DataType { get; set; } = "String";
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
}
