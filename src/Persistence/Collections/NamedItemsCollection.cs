// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

[DebuggerDisplay("Count = {Count}")]
public abstract class NamedItemsCollection<T> : IDictionary<string, T>, IDictionary
{
    private readonly Dictionary<string, T> _items = new();
    private readonly Func<T, string> _keySelector;

    protected NamedItemsCollection(Func<T, string> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    public NamedItemsCollection(IEnumerable<T> values, Func<T, string> keySelector) : this(keySelector)
    {
        foreach (var item in values)
        {
            _items.Add(keySelector(item), item);
        }
    }

    internal NamedItemsCollection(IEnumerable<KeyValuePair<string, T>> values, Func<T, string> keySelector) : this(keySelector)
    {
        foreach (var kvp in values)
        {
            _items.Add(kvp.Key, kvp.Value);
        }
    }

    internal NamedItemsCollection(ReadOnlySpan<KeyValuePair<string, T>> values, Func<T, string> keySelector) : this(keySelector)
    {
        foreach (var kvp in values)
        {
            _items.Add(kvp.Key, kvp.Value);
        }
    }

    public T this[string key]
    {
        get => _items[key];
        set => _items[key] = value;
    }

    public IEnumerable<string> Keys => _items.Keys;

    public IEnumerable<T> Values => _items.Values;

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    ICollection<string> IDictionary<string, T>.Keys => _items.Keys;

    ICollection<T> IDictionary<string, T>.Values => _items.Values;

    public bool IsFixedSize => false;

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
            if (key is string s && value is T v)
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

    public void Add(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        _items.Add(key, value);
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        return _items.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Remove(string key)
    {
        return _items.Remove(key);
    }

    public void Add(KeyValuePair<string, T> kv)
    {
        _items.Add(kv.Key, kv.Value);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool Contains(KeyValuePair<string, T> item)
    {
        return _items.Contains(item);
    }

    public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<string, T> item)
    {
        return _items.Remove(item.Key);
    }

    public void Add(object key, object? value)
    {
        if (key is string s && value is T v)
            Add(s, v);
        throw new ArgumentException();
    }

    public bool Contains(object key)
    {
        if (key is string s)
            return ContainsKey(s);
        if (key is T v)
            return ContainsKey(_keySelector(v));

        throw new ArgumentException();
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return _items.GetEnumerator();
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
