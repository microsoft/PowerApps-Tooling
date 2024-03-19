// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomPropertyParameter
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DataType DataType { get; init; }
    public bool IsRequired { get; init; }
}
