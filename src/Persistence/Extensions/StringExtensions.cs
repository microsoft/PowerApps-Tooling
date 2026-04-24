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
#if NETFRAMEWORK
        return string.Concat(first.ToString(), input[1..]);
#else
        return string.Concat(new ReadOnlySpan<char>(in first), input.AsSpan(1));
#endif
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

    /// <summary>
    /// Converts a string to null if it is empty or whitespace.
    /// </summary>
    public static string? WhiteSpaceToNull(this string? source)
    {
        return string.IsNullOrWhiteSpace(source) ? null : source;
    }

    /// <summary>
    /// Converts a string to null if it is empty.
    /// </summary>
    public static string? EmptyToNull(this string? source)
    {
        return string.IsNullOrEmpty(source) ? null : source;
    }
}
