// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));
    }
}
