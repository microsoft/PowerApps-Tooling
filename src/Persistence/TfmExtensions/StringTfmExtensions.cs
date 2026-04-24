// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System.Globalization;
using System.Text;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmExtensions;

public static class StringTfmExtensions
{
#if NETFRAMEWORK
    public static string Replace(this string source, string oldValue, string? newValue, StringComparison comparisonType)
    {
        // For now, looks like NetFx only supports the Ordinal comparison for Replace
        if (comparisonType == StringComparison.Ordinal)
        {
            return source.Replace(oldValue, newValue);
        }

        // Anything else we'll just throw for now, since we are going for current parity for now
        throw new ArgumentException($"String.Replace with comparisonType {comparisonType} is not supported on .NET Framework. We can consider implementing if needed.", nameof(comparisonType));
    }

    public static bool EndsWith(this string source, char value)
    {
        return source.Length > 0 && source[^1] == value;
    }
#endif

#if !NET6_0_OR_GREATER
    public static StringBuilder AppendLine(this StringBuilder sb, IFormatProvider? provider, FormattableString value)
    {
        return sb.AppendLine(value.ToString(provider));
    }
#endif
}
