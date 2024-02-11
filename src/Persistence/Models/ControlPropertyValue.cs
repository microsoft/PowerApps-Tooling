// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[DebuggerDisplay("{Value}")]
public sealed record ControlPropertyValue
{
    public ControlPropertyValue()
    {
    }

    public ControlPropertyValue(string? value)
    {
        Value = value;
    }

    public string? Value { get; init; }

    public bool IsFormula { get; init; }
}
