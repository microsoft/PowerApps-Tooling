// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public class ControlPropertiesCollection : ReadOnlyDictionary<string, ControlPropertyValue>
{
    public ControlPropertiesCollection(IDictionary<string, ControlPropertyValue> source) : base(source)
    {
    }

    public static implicit operator ControlPropertiesCollection(Dictionary<string, ControlPropertyValue> coll)
    {
        return new ControlPropertiesCollection(coll);
    }

    public static readonly ControlPropertiesCollection Empty = new(new Dictionary<string, ControlPropertyValue>());
}
