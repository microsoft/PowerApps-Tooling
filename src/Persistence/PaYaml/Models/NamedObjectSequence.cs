// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Represents a collection of named objects whose keys are the name of each item.
/// </summary>
public class NamedObjectSequence<TValue> : NamedObjectSequenceBase<string, TValue, NamedObject<TValue>>, INamedObjectCollection<TValue>
    where TValue : notnull
{
    private readonly static StringComparer DefaultComparer = StringComparer.Ordinal;

    public NamedObjectSequence()
        : this(DefaultComparer)
    {
    }

    public NamedObjectSequence(IEqualityComparer<string>? comparer)
        : base(null, comparer ?? DefaultComparer)
    {
    }

    public NamedObjectSequence(IEnumerable<NamedObject<TValue>>? values)
        : this(values, DefaultComparer)
    {
    }

    public NamedObjectSequence(IEnumerable<NamedObject<TValue>>? values, IEqualityComparer<string>? comparer)
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
