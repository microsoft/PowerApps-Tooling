// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// Simple clas for indicating a validated string. This class is only used for the implementation of strong-typed strings.<br/>
/// It allows for strong-typed strings to provide private ctor for validated strings but to also
/// expose a public ctor that does validation in the constructor instead of using the <see cref="IParsable{TSelf}"/> methods.
/// </summary>
/// <typeparam name="TTypedString">The type of the strong-typed string.</typeparam>
public sealed class ValidatedString<TTypedString>
    where TTypedString : ITypedStringValidator
{
    private ValidatedString(string value)
    {
        // TODO - replace validation check
        // Contracts.Assert(TTypedString.IsValid(value), "The value must be validated before constructing a typed string.");
        Value = value;
    }

    /// <summary>
    /// The validated string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates an argument value and returns a validated string.
    /// If the value is not valid, an <see cref="ArgumentException"/> is thrown.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="argumentName">
    /// The name of the argument. When not set, the compiler uses the expression for <paramref name="value"/>.
    /// </param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is invalid.</exception>
    public static ValidatedString<TTypedString> ValidateArgument([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? argumentName = null)
    {
        if (!TryValidate(value, out var validated))
        {
            throw new ArgumentException($"Invalid value for a {typeof(TTypedString).Name}.", argumentName);
        }

        return validated;
    }

    /// <summary>
    /// Validates the input value and returns a validated string.
    /// </summary>
    public static bool TryValidate([NotNullWhen(true)] string? value, [NotNullWhen(true)] out ValidatedString<TTypedString>? validated)
    {
        if (TTypedString.IsValid(value))
        {
            validated = new(value);
            return true;
        }

        validated = null;
        return false;
    }
}
