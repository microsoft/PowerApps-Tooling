// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// A strong-typed string that is guaranteed to not be empty (i.e. <see cref="string.Empty"/>) or whitespace only.
/// </summary>
[TypeConverter(typeof(TypedStringConverter<NonWhitespaceString>))]
[JsonConverter(typeof(TypedStringJsonConverter))]
public sealed record NonWhitespaceString : NonWhitespaceString<NonWhitespaceString>, ITypedStringCreator<NonWhitespaceString>
{
    public NonWhitespaceString(string value) : this(ValidatedString<NonWhitespaceString>.ValidateArgument(value)) { }

    private NonWhitespaceString(ValidatedString<NonWhitespaceString> validatedValue) : base(validatedValue) { }

    public static implicit operator string(NonWhitespaceString name) => name.Value;

    public override string ToString() => Value;

    static NonWhitespaceString ITypedStringCreator<NonWhitespaceString>.CreateFromValid(ValidatedString<NonWhitespaceString> validatedValue) => new(validatedValue);
}

/// <summary>
/// Base class for any strong-typed string that is guaranteed to not be empty (i.e. <see cref="string.Empty"/>) or whitespace only.
/// </summary>
/// <remarks>
/// Implementors must still implement the <see cref="ITypedStringCreator{T}"/> interface.
/// Using this base class removes the need of the final class to implement the <see cref="ITypedStringValidator"/> interface.
/// </remarks>
public abstract record NonWhitespaceString<TSelf> : TypedStringBase<TSelf>, ITypedStringValidator
    where TSelf : NonWhitespaceString<TSelf>, ITypedStringCreator<TSelf>, ITypedStringValidator
{
    protected NonWhitespaceString(ValidatedString<TSelf> validatedValue) : base(validatedValue) { }

    static bool ITypedStringValidator.IsValid([NotNullWhen(true)] string? value) => !string.IsNullOrWhiteSpace(value);
}
