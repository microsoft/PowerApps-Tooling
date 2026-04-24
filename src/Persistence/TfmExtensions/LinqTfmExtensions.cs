// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using YamlDotNet.Core.Tokens;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.TfmExtensions;

public static class LinqTfmExtensions
{
#if !NET7_0_OR_GREATER
    public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) => Order(source, null);

    public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source, IComparer<T>? comparer) => source.OrderBy(static x => x, comparer);
#endif

#if !NET5_0_OR_GREATER
    public static IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
    {
        return first.Zip(second, resultSelector: static (a, b) => (First: a, Second: b));
    }
#endif
}
