// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomPropertyParameter : INamedObject
{
    public required string Name { get; set; } = string.Empty;
    public required string DataType { get; set; } = "String";
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
}
