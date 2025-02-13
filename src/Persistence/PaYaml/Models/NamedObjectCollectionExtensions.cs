// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

public static class NamedObjectCollectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<TValue>([NotNullWhen(false)] this IReadOnlyNamedObjectCollection<TValue>? collection)
        where TValue : notnull
    {
        return collection is null || collection.Count == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedObjectMapping<TValue>? EmptyToNull<TValue>(this NamedObjectMapping<TValue>? collection)
        where TValue : notnull
    {
        return collection?.Count == 0 ? null : collection;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamedObjectSequence<TValue>? EmptyToNull<TValue>(this NamedObjectSequence<TValue>? collection)
        where TValue : notnull
    {
        return collection?.Count == 0 ? null : collection;
    }
}
