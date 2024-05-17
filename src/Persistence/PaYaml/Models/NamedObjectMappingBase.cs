// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Base implementation for an <see cref="INamedObject{TName, TValue}"/> yaml mapping.
/// </summary>
public abstract class NamedObjectMappingBase<TName, TValue, TNamedObject> : INamedObjectCollection<TName, TValue, TNamedObject>, IYamlConvertible
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

    #region IYamlConvertible

    private void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
    {
        Debug.Assert(expectedType.IsAssignableTo(typeof(NamedObjectMappingBase<TName, TValue, TNamedObject>)));

        if (parser.TryConsumeNull())
        {
            // REVIEW: We may not want to support null scalars when reading named object mappings.
            //         This will require us to use a custom converter to be able to have access to return a null value.
            // For now, we think all uses would be benign currently to just treat null inputs as an empty collection:
            return;
        }

        parser.Consume<MappingStart>();
        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var itemStartEvent = parser.Current!;
            var namedObject = ReadNamedObjectFromMappingEntryEvents(parser, nestedObjectDeserializer);

            if (!TryAdd(namedObject))
            {
                var existingNamedObject = GetNamedObject(namedObject.Name);
                throw new YamlException(itemStartEvent.Start, itemStartEvent.End, $"Duplicate name '{namedObject.Name}' used at {itemStartEvent}. First use is located at {existingNamedObject.Start}.");
            }
        }
    }

    private void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
    {
        emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));
        foreach (var namedObject in InnerCollection.Values)
        {
            WriteNamedObjectToMappingEntryEvents(emitter, namedObject, nestedObjectSerializer);
        }

        emitter.Emit(new MappingEnd());
    }

    protected abstract TNamedObject ReadNamedObjectFromMappingEntryEvents(IParser parser, ObjectDeserializer nestedObjectDeserializer);

    protected abstract void WriteNamedObjectToMappingEntryEvents(IEmitter emitter, TNamedObject namedObject, ObjectSerializer nestedObjectSerializer);

    void IYamlConvertible.Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
    {
        Read(parser, expectedType, nestedObjectDeserializer);
    }

    void IYamlConvertible.Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
    {
        Write(emitter, nestedObjectSerializer);
    }

    #endregion
}
