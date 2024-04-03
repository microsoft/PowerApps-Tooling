// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

[DebuggerDisplay("Count = {Count}")]
public class NamedItemsCollection<TValue> :
    IDictionary<string, TValue>,
    IDictionary where
    TValue : notnull
{
    private readonly Dictionary<string, TValue> _items = new();
    private readonly Func<TValue, string> _keySelector;

    protected NamedItemsCollection(Func<TValue, string> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    public NamedItemsCollection(IEnumerable<TValue> values, Func<TValue, string> keySelector) : this(keySelector)
    {
        foreach (var item in values)
        {
            _items.Add(keySelector(item), item);
        }
    }

    internal NamedItemsCollection(IEnumerable<KeyValuePair<string, TValue>> values, Func<TValue, string> keySelector) : this(keySelector)
    {
        foreach (var kvp in values)
        {
            _items.Add(kvp.Key, kvp.Value);
        }
    }

    internal NamedItemsCollection(ReadOnlySpan<KeyValuePair<string, TValue>> values, Func<TValue, string> keySelector) : this(keySelector)
    {
        foreach (var kvp in values)
        {
            _items.Add(kvp.Key, kvp.Value);
        }
    }

    public TValue this[string key]
    {
        get => _items[key];
        set => _items[key] = value;
    }

    public IEnumerable<string> Keys => _items.Keys;

    public IEnumerable<TValue> Values => _items.Values;

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public bool IsFixedSize => false;

    ICollection<string> IDictionary<string, TValue>.Keys => _items.Keys;

    ICollection<TValue> IDictionary<string, TValue>.Values => _items.Values;

    ICollection IDictionary.Keys => _items.Keys;

    ICollection IDictionary.Values => _items.Values;

    public bool IsSynchronized => true;

    public object SyncRoot => _items;

    public object? this[object key]
    {
        get
        {
            if (key is string s)
                return _items[s];
            throw new ArgumentException();
        }
        set
        {
            if (key is string s && value is TValue v)
            {
                _items[s] = v;
                return;
            }

            throw new ArgumentException();
        }
    }

    public bool ContainsKey(string key)
    {
        return _items.ContainsKey(key);
    }

    public void Add(string key, TValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        _items.Add(key, value);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
    {
        return _items.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return ((IDictionary)_items).GetEnumerator();
    }

    public bool Remove(string key)
    {
        return _items.Remove(key);
    }

    public void Add(KeyValuePair<string, TValue> kv)
    {
        _items.Add(kv.Key, kv.Value);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool Contains(KeyValuePair<string, TValue> item)
    {
        return _items.Contains(item);
    }

    public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<string, TValue> item)
    {
        return _items.Remove(item.Key);
    }

    public void Add(object key, object? value)
    {
        if (key is string s && value is TValue v)
            Add(s, v);
        throw new ArgumentException();
    }

    public bool Contains(object key)
    {
        if (key is string s)
            return ContainsKey(s);
        if (key is TValue v)
            return ContainsKey(_keySelector(v));

        throw new ArgumentException();
    }

    public void Remove(object key)
    {
        Remove((string)key);
    }

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }
}
