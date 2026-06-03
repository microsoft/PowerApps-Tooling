// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmExtensions;

public static class MemoryTfmExtensions
{
#if !NET9_0_OR_GREATER
    public static bool ContainsAny(this ReadOnlySpan<char> span, string values)
    {
        return span.ContainsAny(values.AsSpan());
    }

    public static bool ContainsAny(this ReadOnlySpan<char> span, ReadOnlySpan<char> values)
    {
        return span.IndexOfAny(values) >= 0;
    }
#endif
}
