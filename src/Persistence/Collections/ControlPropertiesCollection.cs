// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

[DebuggerDisplay("Count = {Count}")]
public class ControlPropertiesCollection : IReadOnlyDictionary<string, ControlPropertyValue>
{
    private readonly Dictionary<string, ControlPropertyValue> _properties = new();

    public ControlPropertiesCollection()
    {
    }

    public ControlPropertiesCollection(IEnumerable<KeyValuePair<string, ControlPropertyValue>> values)
    {
        foreach (var kvp in values)
        {
            _properties.Add(kvp.Key, kvp.Value);
        }
    }

    public ControlPropertiesCollection(ReadOnlySpan<KeyValuePair<string, ControlPropertyValue>> values)
    {
        foreach (var kvp in values)
        {
            _properties.Add(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Used for collection expressions initialization syntax
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static ControlPropertiesCollection Create(scoped ReadOnlySpan<KeyValuePair<string, ControlPropertyValue>> values)
    {
        return new(values);
    }

    public ControlPropertyValue this[string key] => _properties[key];

    public IEnumerable<string> Keys => _properties.Keys;

    public IEnumerable<ControlPropertyValue> Values => _properties.Values;

    public int Count => _properties.Count;

    public bool ContainsKey(string key)
    {
        return _properties.ContainsKey(key);
    }

    public void Add(string key, ControlPropertyValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        _properties.Add(key, value);
    }

    public void Add(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        _properties.Add(key, new ControlPropertyValue(value));
    }

    public void Add(Tuple<string, string> keyValue)
    {
        if (string.IsNullOrWhiteSpace(keyValue.Item1))
            throw new ArgumentNullException(nameof(keyValue.Item1));

        _properties.Add(keyValue.Item1, new ControlPropertyValue(keyValue.Item2));
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

    public static implicit operator ControlPropertiesCollection(Dictionary<string, ControlPropertyValue> collection)
    {
        return new ControlPropertiesCollection(collection);
    }
}
