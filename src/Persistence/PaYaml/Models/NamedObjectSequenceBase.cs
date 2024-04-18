// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Base implementation for an <see cref="INamedObject{TName, TValue}"/> yaml sequence.
/// </summary>
public abstract class NamedObjectSequenceBase<TName, TValue, TNamedObject> : INamedObjectCollection<TName, TValue, TNamedObject>
    where TName : notnull
    where TValue : notnull
    where TNamedObject : INamedObject<TName, TValue>
{
    private protected NamedObjectSequenceBase(IEnumerable<TNamedObject>? values, IEqualityComparer<TName> comparer)
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

    private protected InnerKeyedCollection InnerCollection { get; }

    public int Count => InnerCollection.Count;

    public IEnumerable<TName> Names => InnerCollection.Names;

    public TNamedObject this[int index] => InnerCollection[index];

    public TValue this[TName name] => InnerCollection[name].Value;

    [SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "ByDesign: 'namedObject' is preferred over 'item'")]
    public void Add(TNamedObject namedObject)
    {
        InnerCollection.Add(namedObject);
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
        return InnerCollection.Contains(name);
    }

    [SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "ByDesign: 'namedObject' is preferred over 'item'")]
    public bool Contains(TNamedObject namedObject)
    {
        return InnerCollection.Contains(namedObject);
    }

    public void CopyTo(TNamedObject[] array, int arrayIndex)
    {
        InnerCollection.CopyTo(array, arrayIndex);
    }

    public IEnumerator<TNamedObject> GetEnumerator()
    {
        return InnerCollection.GetEnumerator();
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
        if (!InnerCollection.TryGetValue(name, out var namedObject))
        {
            return -1;
        }

        return InnerCollection.IndexOf(namedObject);
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
        if (InnerCollection.Contains(namedObject.Name))
        {
            return false;
        }

        InnerCollection.Add(namedObject);
        return true;
    }

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

    internal sealed class InnerKeyedCollection : KeyedCollection<TName, TNamedObject>
    {
        public InnerKeyedCollection(IEqualityComparer<TName>? comparer) : base(comparer)
        {
        }

        protected override TName GetKeyForItem(TNamedObject namedObject)
        {
            return namedObject.Name;
        }

        public IEnumerable<TName> Names => Items.Select(namedObject => namedObject.Name);
    }
}
