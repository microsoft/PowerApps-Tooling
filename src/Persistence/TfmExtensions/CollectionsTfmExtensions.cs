// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using YamlDotNet.Core.Tokens;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmExtensions;

public static class CollectionsTfmExtensions
{
#if NETFRAMEWORK
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }

    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }

        return false;
    }

    public static bool TryGetValue<TKey, TValue>(this KeyedCollection<TKey, TValue> source, TKey key, [MaybeNullWhen(false)] out TValue item)
    {
        if (!source.Contains(key))
        {
            item = default;
            return false;
        }

        item = source[key];
        return true;
    }

    public static TValue GetValueAtIndex<TKey, TValue>(this SortedList<TKey, TValue> source, int index)
    {
        // Make sure we use the indexer with the right type, not keys indexer.
        return source.Values[index];
    }
#endif
}
