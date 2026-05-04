// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Base implementation for an <see cref="INamedObject{TName, TValue}"/> yaml mapping.
/// </summary>
public abstract class NamedObjectMappingBase<TName, TValue, TNamedObject> : INamedObjectCollection<TName, TValue, TNamedObject>
    where TName : notnull
    where TValue : notnull
    where TNamedObject : INamedObject<TName, TValue>
{
    private protected NamedObjectMappingBase(IEnumerable<TNamedObject>? values, IComparer<TName> comparer)
    {
        InnerCollection = new(comparer);
        if (values is not null)
        {
            foreach (var namedObject in values)
            {
                Add(namedObject);
            }
        }
    }

    private protected SortedList<TName, TNamedObject> InnerCollection { get; }

    public int Count => InnerCollection.Count;

    public IEnumerable<TName> Names => InnerCollection.Keys;

    public TNamedObject this[int index] => InnerCollection.GetValueAtIndex(index);

    public TValue this[TName name]
    {
        get => GetValue(name);
        set
        {
            _ = value ?? throw new ArgumentNullException(nameof(value));
            InnerCollection[name] = CreateNamedObject(name, value);
        }
    }

    [SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "ByDesign: 'namedObject' is preferred over 'item'")]
    public void Add(TNamedObject namedObject)
    {
        InnerCollection.Add(namedObject.Name, namedObject);
    }

    public void Add(TName name, TValue value)
    {
        _ = name ?? throw new ArgumentNullException(nameof(name));
        _ = value ?? throw new ArgumentNullException(nameof(value));

        Add(CreateNamedObject(name, value));
    }

    /// <summary>
    /// When implemented by a derived class, will create a new named object instance for the specified name and value.
    /// </summary>
    protected abstract TNamedObject CreateNamedObject(TName name, TValue value);

    public void Clear()
    {
        InnerCollection.Clear();
    }

    public bool Contains(TName name)
    {
        return InnerCollection.ContainsKey(name);
    }

    [SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "ByDesign: 'namedObject' is preferred over 'item'")]
    public bool Contains(TNamedObject namedObject)
    {
        return InnerCollection.ContainsValue(namedObject);
    }

    public void CopyTo(TNamedObject[] array, int arrayIndex)
    {
        InnerCollection.Values.CopyTo(array, arrayIndex);
    }

    public IEnumerator<TNamedObject> GetEnumerator()
    {
        return InnerCollection.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public TNamedObject GetNamedObject(TName name)
    {
        return InnerCollection[name];
    }

    public TValue GetValue(TName name)
    {
        return InnerCollection[name].Value;
    }

    public int IndexOf(TName name)
    {
        return InnerCollection.IndexOfKey(name);
    }

    public bool Remove(TName name)
    {
        return InnerCollection.Remove(name);
    }

    public void RemoveAt(int index)
    {
        InnerCollection.RemoveAt(index);
    }

    public bool TryAdd(TNamedObject namedObject)
    {
        return InnerCollection.TryAdd(namedObject.Name, namedObject);
    }

#if NETFRAMEWORK
    // Default interface implementations are not supported on .NET Framework.
    // Provide explicit implementations here so the type satisfies ICollection<TNamedObject>.
    bool ICollection<TNamedObject>.IsReadOnly => false;

    bool ICollection<TNamedObject>.Remove(TNamedObject item)
    {
        throw new NotSupportedException("Remove of a named object instance is not supported. Use Remove overload that takes the name of the item you want to remove instead.");
    }
#endif

    public bool TryGetNamedObject(TName name, [MaybeNullWhen(false)] out TNamedObject namedObject)
    {
        return InnerCollection.TryGetValue(name, out namedObject);
    }

    public bool TryGetValue(TName name, [MaybeNullWhen(false)] out TValue value)
    {
        if (InnerCollection.TryGetValue(name, out var namedObject))
        {
            value = namedObject.Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
