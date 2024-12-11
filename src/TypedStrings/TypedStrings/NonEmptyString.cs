// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// A strong-typed string that is guaranteed to not be empty (i.e. <see cref="string.Empty"/>).
/// </summary>
[TypeConverter(typeof(TypedStringConverter<NonEmptyString>))]
[JsonConverter(typeof(TypedStringJsonConverter))]
public sealed record NonEmptyString : NonEmptyString<NonEmptyString>, ITypedStringCreator<NonEmptyString>
{
    public NonEmptyString(string value) : this(ValidatedString<NonEmptyString>.ValidateArgument(value)) { }

    private NonEmptyString(ValidatedString<NonEmptyString> validatedValue) : base(validatedValue) { }

    public static implicit operator string(NonEmptyString name) => name.Value;

    public override string ToString() => Value;

    static NonEmptyString ITypedStringCreator<NonEmptyString>.CreateFromValid(ValidatedString<NonEmptyString> validatedValue) => new(validatedValue);
}

/// <summary>
/// Base class for any strong-typed string that is guaranteed to not be empty (i.e. <see cref="string.Empty"/>).
/// </summary>
/// <remarks>
/// Implementors must still implement the <see cref="ITypedStringCreator{T}"/> interface.
/// Using this base class removes the need of the final class to implement the <see cref="ITypedStringValidator"/> interface.
/// </remarks>
public abstract record NonEmptyString<TSelf> : TypedStringBase<TSelf>, ITypedStringValidator
    where TSelf : NonEmptyString<TSelf>, ITypedStringCreator<TSelf>, ITypedStringValidator
{
    protected NonEmptyString(ValidatedString<TSelf> validatedValue) : base(validatedValue) { }

    static bool ITypedStringValidator.IsValid([NotNullWhen(true)] string? value) => !string.IsNullOrEmpty(value);
}
