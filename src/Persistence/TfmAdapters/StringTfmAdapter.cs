// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmAdapters;

/// <summary>
/// Most of these adapters add nullable checks which aren't available on net48
/// </summary>
public static class StringTfmAdapter
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] string? value) => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] string? value) => string.IsNullOrWhiteSpace(value);

    public static string Join(char separator, params string?[] values)
    {
#if NETFRAMEWORK
        return string.Join(separator.ToString(), values);
#else
        return string.Join(separator, values);
#endif
    }
}
