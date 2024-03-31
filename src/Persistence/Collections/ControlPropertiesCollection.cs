// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

[DebuggerDisplay("Count = {Count}")]
public class ControlPropertiesCollection : IReadOnlyDictionary<string, ControlProperty>
{
    private readonly Dictionary<string, ControlProperty> _properties = new();

    public ControlPropertiesCollection()
    {
    }

    public ControlPropertiesCollection(IEnumerable<KeyValuePair<string, ControlProperty>> values)
    {
        foreach (var kvp in values)
        {
            _properties.Add(kvp.Key, kvp.Value);
        }
    }

    public ControlPropertiesCollection(IEnumerable<ControlProperty> values)
    {
        foreach (var kvp in values)
        {
            _properties.Add(kvp.Name, kvp);
        }
    }

    public ControlPropertiesCollection(ReadOnlySpan<KeyValuePair<string, ControlProperty>> values)
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
    public static ControlPropertiesCollection Create(scoped ReadOnlySpan<KeyValuePair<string, ControlProperty>> values)
    {
        return new(values);
    }

    public static ControlPropertiesCollection Create(IEnumerable<ControlProperty> values)
    {
        return new(values);
    }

    public ControlProperty this[string key]
    {
        get => _properties[key];
        set => _properties[key] = value;
    }

    public IEnumerable<string> Keys => _properties.Keys;

    public IEnumerable<ControlProperty> Values => _properties.Values;

    public int Count => _properties.Count;

    public bool ContainsKey(string key)
    {
        return _properties.ContainsKey(key);
    }

    public void Add(string key, ControlProperty value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        _properties.Add(key, value);
    }

    public void Add(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        _properties.Add(key, new ControlProperty(key, value));
    }

    public void Add(Tuple<string, string> keyValue)
    {
        if (string.IsNullOrWhiteSpace(keyValue.Item1))
            throw new ArgumentNullException(nameof(keyValue.Item1));

        _properties.Add(keyValue.Item1, new ControlProperty(keyValue.Item1, keyValue.Item2));
    }

    public void Remove(string key)
    {
        _properties.Remove(key);
    }

    public IEnumerator<KeyValuePair<string, ControlProperty>> GetEnumerator()
    {
        return _properties.GetEnumerator();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ControlProperty value)
    {
        return _properties.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static implicit operator ControlPropertiesCollection(Dictionary<string, ControlProperty> collection)
    {
        return new ControlPropertiesCollection(collection);
    }
}
