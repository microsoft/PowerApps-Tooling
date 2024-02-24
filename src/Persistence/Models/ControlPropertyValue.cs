// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[DebuggerDisplay("{Value}")]
public sealed record ControlPropertyValue
{
    public const char FormulaPrefix = '=';

    public ControlPropertyValue()
    {
    }

    public ControlPropertyValue(string? value)
    {
        Value = value;
    }

    public static ControlPropertyValue FromTextFirstString(string? value)
    {
        if (value == null)
            return new ControlPropertyValue();

        // If the value starts with the formula prefix, then it is a formula.
        if (value.StartsWith(FormulaPrefix))
            return new ControlPropertyValue(value[1..]) { IsFormula = true };

        // Otherwise, it is a string value which should be in quotes.
        return new ControlPropertyValue($"\"{value}\"");
    }

    public string? Value { get; init; }

    public bool IsFormula { get; init; }
}
