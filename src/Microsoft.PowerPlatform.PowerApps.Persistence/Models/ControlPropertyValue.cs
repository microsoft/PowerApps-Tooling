// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public sealed record ControlPropertyValue
{
    public string? Value { get; init; }

    public bool IsFormula { get; init; }
}
