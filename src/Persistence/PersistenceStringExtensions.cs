// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal static class PersistenceStringExtensions
{
    public static string? EmptyToNull(this string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
