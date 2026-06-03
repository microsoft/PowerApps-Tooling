// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.Extensions;

public static class StringExtensions
{
    public static string UnEscapePAString(this string text)
    {
        return text[1..^1].Replace("\"\"", "\"");
    }

    public static string EscapePAString(this string text)
    {
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    public static string FirstCharToUpper(this string input)
    {
        ThrowIfNullOrEmpty(input);

        return $"{char.ToUpper(input[0])}{input[1..]}";
    }
}
