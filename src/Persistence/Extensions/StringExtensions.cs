// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var first = char.ToUpper(input[0], CultureInfo.InvariantCulture);
        var rest = input.AsSpan(1);
        return string.Concat(new ReadOnlySpan<char>(ref first), rest);
    }

    public static bool StartsWithInvariant(this string input, string value)
    {
        _ = input ?? throw new ArgumentNullException(nameof(input));

        return input.StartsWith(value, StringComparison.InvariantCulture);
    }

    public static bool StartsWithOrdinal(this string input, string value)
    {
        _ = input ?? throw new ArgumentNullException(nameof(input));

        return input.StartsWith(value, StringComparison.Ordinal);
    }
}
