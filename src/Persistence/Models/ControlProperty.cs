// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[DebuggerDisplay("{Value}")]
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "REVIEW: Author of this class should remove this suppression and fix the violation, or update this Justification message.")]
public sealed record ControlProperty : IComparable<ControlProperty>
{
    public const char FormulaPrefix = '=';

    public ControlProperty()
    {
    }

    [SetsRequiredMembers]
    public ControlProperty(string name, string? value)
    {
        Name = name;
        Value = value;
    }

    public static ControlProperty FromTextFirstString(string name, string? value)
    {
        if (value == null)
            return new ControlProperty(name, $"\"\"");

        // If the value starts with the formula prefix, then it is a formula.
        if (value.StartsWith(FormulaPrefix))
            return new ControlProperty(name, value[1..]) { IsFormula = true };

        // Otherwise, it is a string value which should be in quotes.
        return new ControlProperty(name, $"\"{value}\"");
    }

    public int CompareTo(ControlProperty? other)
    {
        if (other == null)
            return 1;

        if (Category != other.Category)
            return Category.CompareTo(other.Category);

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    public static implicit operator ControlProperty(KeyValuePair<string, string> property)
    {
        return new ControlProperty(property.Key, property.Value);
    }

    public required string Name { get; init; }

    public string? Value { get; init; }

    public bool IsFormula { get; init; }

    public PropertyCategory Category { get; init; } = PropertyCategory.Unknown;
}
