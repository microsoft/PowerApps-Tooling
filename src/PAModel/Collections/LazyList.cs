// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.Collections;

/// <summary>
/// Allows the accumulation of a large number of individual elements,
/// which can then be combined into a single collection at the end of
/// the operation without the creation of many intermediate large
/// memory blocks.
/// </summary>
internal class LazyList<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> values;

    public static readonly LazyList<T> Empty = new([]);

    public LazyList(IEnumerable<T> values)
    {
        this.values = values;
    }

    public LazyList(T value)
    {
        values = new[] { value };
    }

    /// <summary>
    /// Gives a new list with the given elements after the elements in this list.
    /// </summary>
    public LazyList<T> With(IEnumerable<T> values)
    {
        if (!values.Any())
            return this;
        return new LazyList<T>(this.values.Concat(values));
    }

    /// <summary>
    /// Gives a new list with the given elements after the elements in this list.
    /// </summary>
    public LazyList<T> With(params T[] values)
    {
        if (!values.Any())
            return this;
        return new LazyList<T>(this.values.Concat(values));
    }

    public IEnumerator<T> GetEnumerator()
    {
        return values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return values.GetEnumerator();
    }

    /// <summary>
    /// Create a new LazyList with the given starting set of values.
    /// </summary>
    public static LazyList<T> Of(params T[] values)
    {
        return new LazyList<T>(values);
    }

    /// <summary>
    /// Create a new LazyList with the given starting set of values.
    /// </summary>
    public static LazyList<T> Of(IEnumerable<T> values)
    {
        return new LazyList<T>(values);
    }
}
