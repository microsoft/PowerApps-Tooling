// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.Extensions;

public static class StringExtensions
{
    public static string UnEscapePAString(this string text)
    {
        return text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
    }

    public static string EscapePAString(this string text)
    {
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    public static string FirstCharToUpper(this string input)
    {
        return input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => input.First().ToString().ToUpper() + input.Substring(1)
        };
    }
}
