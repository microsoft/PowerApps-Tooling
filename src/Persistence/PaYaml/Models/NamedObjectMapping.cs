// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Represents a collection of named objects that are sorted by their names.
/// </summary>
public class NamedObjectMapping<TValue> : NamedObjectMappingBase<string, TValue, NamedObject<TValue>>, INamedObjectCollection<TValue>
    where TValue : notnull
{
    private static readonly StringComparer DefaultComparer = StringComparer.Ordinal;

    public NamedObjectMapping()
        : this(DefaultComparer)
    {
    }

    public NamedObjectMapping(IComparer<string>? comparer)
        : base(null, comparer ?? DefaultComparer)
    {
    }

    public NamedObjectMapping(IEnumerable<NamedObject<TValue>>? values)
        : this(values, DefaultComparer)
    {
    }

    public NamedObjectMapping(IEnumerable<NamedObject<TValue>>? values, IComparer<string>? comparer)
        : base(values, comparer ?? DefaultComparer)
    {
    }

    protected override NamedObject<TValue> CreateNamedObject(string name, TValue value)
    {
        _ = name ?? throw new ArgumentNullException(nameof(name));
        _ = value ?? throw new ArgumentNullException(nameof(value));

        return new NamedObject<TValue>(name, value);
    }
}
