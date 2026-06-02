// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace Microsoft.PowerPlatform.Formulas.Tools.Extensions;

public static class CollectionsExtensions
{
    public static void AddRange<TKey, TValue>(
        this IDictionary<TKey, TValue> thisDictionary,
        IEnumerable<KeyValuePair<TKey, TValue>> other)
    {
        foreach (var kv in other)
        {
            thisDictionary[kv.Key] = kv.Value;
        }
    }

    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default)
        where TValue : new()
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }
        return @default;
    }

    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        where TValue : new()
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }
        value = new TValue();
        dict[key] = value;
        return value;
    }

    public static IList<T> Clone<T>(this IList<T> obj)
        where T : ICloneable<T>
    {
        return obj?.Select(item => item.Clone()).ToList();
    }

}
