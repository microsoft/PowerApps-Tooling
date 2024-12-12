// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// A strong-typed string that is guaranteed to not be empty (i.e. <see cref="string.Empty"/>).
/// </summary>
// [TypedString]
public sealed partial record NonEmptyString
{
    private static bool IsValid([NotNullWhen(true)] string? value) => !string.IsNullOrEmpty(value);
}



// Generated code:
public sealed partial record NonEmptyString : ITypedString<NonEmptyString>
{
    public NonEmptyString(string value)
    {
        Value = ValidateArgument(value);
    }

    public string Value { get; }

    public static implicit operator string(NonEmptyString name) => name.Value;

    public override string ToString() => Value;

    private static string ValidateArgument([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
    {
        if (!TryValidate(value, out var validated))
        {
            throw new ArgumentException($"Invalid value for a {nameof(NonEmptyString)}.", argumentName);
        }

        return validated;
    }

    /// <summary>
    /// Validates the input value and returns a validated string.
    /// </summary>
    private static bool TryValidate([NotNullWhen(true)] string? value, [NotNullWhen(true)] out string? validated)
    {
        if (IsValid(value))
        {
            validated = value;
            return true;
        }

        validated = null;
        return false;
    }

    static NonEmptyString IParsable<NonEmptyString>.Parse(string s, IFormatProvider? provider)
    {
        // TODO
        throw new NotImplementedException();
    }

    static bool IParsable<NonEmptyString>.TryParse(string? s, IFormatProvider? provider, out NonEmptyString result)
    {
        // TODO
        throw new NotImplementedException();
    }
}
