// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Represents an collection of <see cref="NamedObject{TValue}"/> containing uniquely named objects.
/// </summary>
public interface INamedObjectCollection<TValue> : INamedObjectCollection<string, TValue, NamedObject<TValue>>, IReadOnlyNamedObjectCollection<TValue>
    where TValue : notnull
{
}

/// <summary>
/// Represents a read-only collection of <see cref="NamedObject{TValue}"/> containing uniquely named objects.
/// </summary>
public interface IReadOnlyNamedObjectCollection<TValue> : IReadOnlyNamedObjectCollection<string, TValue, NamedObject<TValue>>
    where TValue : notnull
{
}

/// <summary>
/// Represents a collection of <see cref="INamedObject{TName, TValue}"/> containing uniquely named objects.
/// </summary>
public interface INamedObjectCollection<TName, TValue, TNamedObject> : ICollection<TNamedObject>, IReadOnlyNamedObjectCollection<TName, TValue, TNamedObject>
    where TName : notnull
    where TValue : notnull
    where TNamedObject : INamedObject<TName, TValue>
{
    void RemoveAt(int index);

    bool Remove(TName name);

    bool TryAdd(TNamedObject namedObject);

    bool ICollection<TNamedObject>.IsReadOnly => throw new NotImplementedException();

    // TODO: Verify this actually hides the member from public API
    // We use default implementations for these methods to try and hide them from the public API.
    bool ICollection<TNamedObject>.Remove(TNamedObject item)
    {
        throw new NotSupportedException("Remove of a named object instance is not supported. Use Remove overload that takes the name of the item you want to remove instead.");
    }

    // CONSIDER: Also use default implementation for ICollection<T>.Contains(T) to discurage usage.
}

/// <summary>
/// Represents a read-only collection of <see cref="INamedObject{TName, TValue}"/> containing uniquely named objects.
/// </summary>
public interface IReadOnlyNamedObjectCollection<TName, TValue, TNamedObject> : IReadOnlyCollection<TNamedObject>
    where TName : notnull
    where TValue : notnull
    where TNamedObject : INamedObject<TName, TValue>
{
    IEnumerable<TName> Names { get; }

    TNamedObject this[int index] { get; } // Set is not supported, as the order of items is determined by the implementation.

    TValue this[TName name] { get; }

    bool Contains(TName name);

    TNamedObject GetNamedObject(TName name);

    TValue GetValue(TName name);

    int IndexOf(TName name);

    bool TryGetNamedObject(TName name, [MaybeNullWhen(false)] out TNamedObject namedObject);

    bool TryGetValue(TName name, [MaybeNullWhen(false)] out TValue value);
}
