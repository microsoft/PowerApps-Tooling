// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public sealed record ControlProperty
{
    // public string Name { get; init; }

    public string? Value { get; init; }

    public bool IsFormula { get; init; }
}
