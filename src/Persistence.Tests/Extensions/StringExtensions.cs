// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Persistence.Tests.Extensions;

internal static class StringExtensions
{
    public static string NormalizeNewlines(this string x)
    {
        return x.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
