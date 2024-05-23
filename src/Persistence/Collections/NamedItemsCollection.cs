// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

[DebuggerDisplay("Count = {Count}")]
public abstract class NamedItemsCollection<TValue>(Func<TValue, string> keySelector) :
    IDictionary<string, TValue>,
    IDictionary
    where
    TValue : notnull
{
    private readonly Dictionary<string, TValue> _items = new();
    private readonly Func<TValue, string> _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));

    protected NamedItemsCollection(IEnumerable<TValue> values, Func<TValue, string> keySelector) : this(keySelector)
    {
        foreach (var item in values)
        {
            _items.Add(keySelector(item), item);
        }
    }

    protected NamedItemsCollection(IEnumerable<KeyValuePair<string, TValue>> values, Func<TValue, string> keySelector) : this(keySelector)
    {
        foreach (var kvp in values)
        {
            _items.Add(kvp.Key, kvp.Value);
        }
    }

    protected NamedItemsCollection(ReadOnlySpan<KeyValuePair<string, TValue>> values, Func<TValue, string> keySelector) : this(keySelector)
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
            if (key is not string s)
                throw new ArgumentException("Key must be a string", nameof(key));

            return _items[s];
        }
        set
        {
            if (key is not string s)
                throw new ArgumentException("Key must be a string", nameof(key));
            if (key is not TValue v)
                throw new ArgumentException($"Value must be of type {typeof(TValue).Name}", nameof(value));

            _items[s] = v;
        }
    }

    public bool ContainsKey(string key)
    {
        return _items.ContainsKey(key);
    }

    public void Add(TValue value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        _items.Add(_keySelector(value), value);
    }

    public void Add(string key, TValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        _items.Add(key, value);
    }

    public void Add(KeyValuePair<string, TValue> item)
    {
        _items.Add(item.Key, item.Value);
    }

    public void Add(object key, object? value)
    {
        if (key is not string s)
            throw new ArgumentException("Key must be a string", nameof(key));
        if (key is not TValue v)
            throw new ArgumentException($"Value must be of type {typeof(TValue).Name}", nameof(value));

        Add(s, v);
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

    public bool Remove(KeyValuePair<string, TValue> item)
    {
        return _items.Remove(item.Key);
    }

    public bool Remove(TValue value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        return _items.Remove(_keySelector(value));
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
        ((IDictionary<string, TValue>)_items).CopyTo(array, arrayIndex);
    }

    public bool Contains(object key)
    {
        if (key is string s)
            return ContainsKey(s);
        if (key is TValue v)
            return ContainsKey(_keySelector(v));

        throw new ArgumentException($"Key must be a string or of type {typeof(TValue).Name}", nameof(key));
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
