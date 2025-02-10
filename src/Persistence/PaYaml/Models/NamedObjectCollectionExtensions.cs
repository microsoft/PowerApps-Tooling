// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

public static class NamedObjectCollectionExtensions
{
    public static NamedObjectMapping<TValue>? EmptyToNull<TValue>(this NamedObjectMapping<TValue>? collection)
        where TValue : notnull
    {
        return collection?.Count == 0 ? null : collection;
    }

    public static NamedObjectSequence<TValue>? EmptyToNull<TValue>(this NamedObjectSequence<TValue>? collection)
        where TValue : notnull
    {
        return collection?.Count == 0 ? null : collection;
    }
}
