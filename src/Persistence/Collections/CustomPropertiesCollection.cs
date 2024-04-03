// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

public class CustomPropertiesCollection : NamedItemsCollection<CustomProperty>
{
    private readonly static Func<CustomProperty, string> _keySelector = v => v.Name;

    public CustomPropertiesCollection() : base(_keySelector)
    {
    }

    public CustomPropertiesCollection(IEnumerable<CustomProperty> values) : base(values, _keySelector)
    {
    }

    internal CustomPropertiesCollection(IEnumerable<KeyValuePair<string, CustomProperty>> values) : base(values, _keySelector) { }

    internal CustomPropertiesCollection(ReadOnlySpan<KeyValuePair<string, CustomProperty>> values) : base(values, _keySelector) { }

    public static implicit operator CustomPropertiesCollection(Dictionary<string, CustomProperty> dictionary)
    {
        return new CustomPropertiesCollection(dictionary);
    }
}
