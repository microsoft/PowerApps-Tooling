// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class DictionaryExtensions
{
    public static bool TryGetValue<T>(this Dictionary<string, object?> dictionary, string name, [MaybeNullWhen(false)] out T? value)
    {
        value = default;
        if (!dictionary.TryGetValue(name, out var valueObj))
            return false;

        if (valueObj == null)
            return true;

        if (valueObj is T)
        {
            value = (T)valueObj;
            return true;
        }

        throw new InvalidCastException($"Value for key '{name}' is not of type '{typeof(T).Name}'.");
    }
}
