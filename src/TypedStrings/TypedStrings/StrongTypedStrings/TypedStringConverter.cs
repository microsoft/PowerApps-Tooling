// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.PowerPlatform.TypedStrings.StrongTypedStrings;

/// <summary>
/// A type converter which uses the target type's <see cref="IParsable{TSelf}"/> implementation.
/// </summary>
public class TypedStringConverter<TTypedString> : TypeConverter
    where TTypedString : ITypedString<TTypedString>
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s)
        {
            if (TTypedString.TryParse(s, out var result))
            {
                return result;
            }
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType) => destinationType == typeof(string);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is TTypedString typedString)
        {
            // Note: We don't use the ToString() method here, as that is used for display purposes which,
            // for record types is not the same as just the Value.
            return typedString.Value;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
