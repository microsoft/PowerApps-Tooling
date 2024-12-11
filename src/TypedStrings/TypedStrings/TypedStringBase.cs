// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.TypedStrings;

/// <summary>
/// The base record for typed strings which implements the common shared logic.<br/>
/// Implementers must implement the <see cref="IParsable{TSelf}"/> interface.<br/>
/// <br/>
/// The semantics of a typed string is that it is not null, provides semantic type safety,
/// and is guaranteed to have been validated.<br/>
/// </summary>
/// <remarks>
/// <para>
/// Benefits of strong-typed strings:<br/>
/// - Parse, don't validate - https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/<br/>
/// - Semantic names for types adds to code readability and maintainability<br/>
/// - Semantic type names allow for better parameter matching in methods which otherwise would have multiple string parameters<br/>
/// - Strong-typed strings are not nullable, which can help avoid null reference exceptions<br/>
/// - When combined with nullable reference types and the `required` modifier, removes the need for validating of inputs within constructors, instead simply assign the required property.<br/>
/// - Parameter name of strong-typed strings can now utilize shorter names, as they no longer need to imply hungarian like notations to be self-documenting.<br/>
/// </para>
/// <para>
/// In the future, we should consider creating a Rosyln code generator which would generate
/// the boilerplate code for the typed strings without needing a base class. It would also
/// allow us to auto-implement the <see cref="IParsable{TSelf}"/> interface w/o the need for
/// the ParseCore/TryParseCore methods and its delegates.
/// </para>
/// </remarks>
[DebuggerDisplay("{GetDebuggerDisplay}")]
public abstract record TypedStringBase<TSelf> : ITypedString<TSelf>, IConvertible, IComparable<TSelf>
    where TSelf : TypedStringBase<TSelf>, ITypedStringCreator<TSelf>, ITypedStringValidator
{
    protected TypedStringBase(ValidatedString<TSelf> validatedValue)
    {
        Value = validatedValue.Value;
    }

    public string Value { get; }

    private string GetDebuggerDisplay => $"{typeof(TSelf).Name} {{ Value = {Value} }}";

    #region IConvertible

    TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

    string IConvertible.ToString(IFormatProvider? provider) => Value;

    object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => Convert.ChangeType(Value, conversionType, provider);

    bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(Value, provider);

    byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(Value, provider);

    char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(Value, provider);

    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => Convert.ToDateTime(Value, provider);

    decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(Value, provider);

    double IConvertible.ToDouble(IFormatProvider? provider) => Convert.ToDouble(Value, provider);

    short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(Value, provider);

    int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(Value, provider);

    long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(Value, provider);

    sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(Value, provider);

    float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(Value, provider);

    ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(Value, provider);

    uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(Value, provider);

    ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(Value, provider);

    #endregion

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "Interface supplies no formatter to pass down.")]
    public virtual int CompareTo(TSelf? other) => Value.CompareTo(other?.Value);

    public static TSelf Parse(string value)
    {
        _ = value ?? throw new ArgumentNullException(nameof(value));

        if (!TryParse(value, out var parsed))
        {
            throw new FormatException($"Invalid string format for a {typeof(TSelf).Name}.");
        }

        return parsed;
    }

    public static bool TryParse([NotNullWhen(true)] string? value, [MaybeNullWhen(false)] out TSelf result)
    {
        if (!ValidatedString<TSelf>.TryValidate(value, out var validated))
        {
            result = default;
            return false;
        }

        result = TSelf.CreateFromValid(validated);
        return true;
    }

    static TSelf IParsable<TSelf>.Parse(string s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    static bool IParsable<TSelf>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result)
    {
        return TryParse(s, out result);
    }
}
