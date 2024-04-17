// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class ListExtensions
{
    /// <summary>
    /// Sorts an IList in place.
    /// </summary>
    public static void Sort<T>(this IList<T> list, Comparison<T> comparison)
    {
        ArrayList.Adapter((IList)list).Sort(new ComparisonComparer<T>(comparison));
    }

    /// <summary>
    /// Convenience method on IEnumerable to allow passing of a Comparison delegate to the OrderBy method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> list, Comparison<T> comparison)
    {
        return list.OrderBy(t => t, new ComparisonComparer<T>(comparison));
    }
}

/// <summary>
/// Wraps a generic Comparison delegate in an IComparer
/// </summary>
/// <typeparam name="T"></typeparam>
public class ComparisonComparer<T> : IComparer<T>, IComparer
{
    private readonly Comparison<T> _comparison;

    public ComparisonComparer(Comparison<T> comparison)
    {
        _comparison = comparison;
    }

    public int Compare(T? x, T? y)
    {
        if (x == null && y == null)
            return 0;
        if (x == null)
            return -1;
        if (y == null)
            return 1;

        return _comparison(x, y);
    }

    public int Compare(object? x, object? y)
    {
        if (x == null && y == null)
            return 0;
        if (x == null)
            return -1;
        if (y == null)
            return 1;

        return _comparison((T)x, (T)y);
    }
}
