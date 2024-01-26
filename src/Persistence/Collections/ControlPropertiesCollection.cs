// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

public class ControlPropertiesCollection : IReadOnlyDictionary<string, ControlPropertyValue>
{
    private readonly Dictionary<string, ControlPropertyValue> _properties = new();

    public ControlPropertiesCollection()
    {
    }

    public ControlPropertiesCollection(IEnumerable<KeyValuePair<string, ControlPropertyValue>> coll)
    {
        foreach (var kvp in coll)
        {
            _properties.Add(kvp.Key, kvp.Value);
        }
    }

    public ControlPropertyValue this[string key] => _properties[key];

    public IEnumerable<string> Keys => _properties.Keys;

    public IEnumerable<ControlPropertyValue> Values => _properties.Values;

    public int Count => _properties.Count;

    public bool ContainsKey(string key)
    {
        return _properties.ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<string, ControlPropertyValue>> GetEnumerator()
    {
        return _properties.GetEnumerator();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ControlPropertyValue value)
    {
        return _properties.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static implicit operator ControlPropertiesCollection(Dictionary<string, ControlPropertyValue> coll)
    {
        return new ControlPropertiesCollection(coll);
    }
}
