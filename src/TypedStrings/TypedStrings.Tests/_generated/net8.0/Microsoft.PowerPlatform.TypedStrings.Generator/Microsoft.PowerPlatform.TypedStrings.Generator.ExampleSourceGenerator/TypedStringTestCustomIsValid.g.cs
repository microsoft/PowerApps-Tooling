﻿// <auto-generated />
#nullable enable

using global::System.Diagnostics.CodeAnalysis;
using global::System.Runtime.CompilerServices;
using global::Microsoft.PowerPlatform.TypedStrings;

namespace Microsoft.PowerPlatform.TypedStrings.Generator;

public partial record TypedStringTestCustomIsValid : ITypedString<TypedStringTestCustomIsValid>
{
    public TypedStringTestCustomIsValid(string value)
    {
        Value = ValidateArgument(value);
    }

    public string Value { get; }

    public static implicit operator string(TypedStringTestCustomIsValid name) => name.Value;

    

    public override string ToString() => Value;

    private static string ValidateArgument([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
    {
        if (!TryValidate(value, out var validated))
        {
            throw new ArgumentException($"Invalid value for a {nameof(TypedStringTestCustomIsValid)}.", argumentName);
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
            global::System.Diagnostics.Debug.Assert(value != null); // Verify `IsValid` is "well behaved".

            validated = value;
            return true;
        }

        validated = null;
        return false;
    }

    static TypedStringTestCustomIsValid IParsable<TypedStringTestCustomIsValid>.Parse(string s, IFormatProvider? provider)
    {
        // TODO
        throw new NotImplementedException();
    }

    static bool IParsable<TypedStringTestCustomIsValid>.TryParse(string? s, IFormatProvider? provider, out TypedStringTestCustomIsValid result)
    {
        // TODO
        throw new NotImplementedException();
    }
}