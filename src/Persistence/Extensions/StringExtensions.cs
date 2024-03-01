// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var first = char.ToUpper(input[0]);
        var rest = input.AsSpan(1);
        return string.Concat(new ReadOnlySpan<char>(first), rest);
    }
}
