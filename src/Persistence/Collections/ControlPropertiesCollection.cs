// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

public class ControlPropertiesCollection : NamedItemsCollection<ControlProperty>
{
    private readonly static Func<ControlProperty, string> _keySelector = v => v.Name;

    public ControlPropertiesCollection() : base(_keySelector)
    {
    }

    public ControlPropertiesCollection(IEnumerable<ControlProperty> values) : base(values, _keySelector)
    {
    }

    internal ControlPropertiesCollection(ReadOnlySpan<KeyValuePair<string, ControlProperty>> values) : base(values, _keySelector)
    {
    }

    internal ControlPropertiesCollection(IEnumerable<KeyValuePair<string, ControlProperty>> values) : base(values, _keySelector)
    {
    }

    /// <summary>
    /// Used for collection expressions initialization syntax
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    internal static ControlPropertiesCollection Create(scoped ReadOnlySpan<KeyValuePair<string, ControlProperty>> values)
    {
        return new(values);
    }

    public void Add(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        Add(key, new ControlProperty(key, value));
    }

    public void Add(Tuple<string, string> keyValue)
    {
        if (string.IsNullOrWhiteSpace(keyValue.Item1))
            throw new ArgumentNullException(nameof(keyValue), "The key cannot be null or whitespace.");

        Add(keyValue.Item1, new ControlProperty(keyValue.Item1, keyValue.Item2));
    }

    public static implicit operator ControlPropertiesCollection(Dictionary<string, ControlProperty> collection)
    {
        return new ControlPropertiesCollection(collection);
    }
}
