// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

public class CustomPropertyParametersCollection : NamedItemsCollection<CustomPropertyParameter>
{
    private readonly static Func<CustomPropertyParameter, string> _keySelector = v => v.Name;

    public CustomPropertyParametersCollection() : base(_keySelector)
    {
    }

    public CustomPropertyParametersCollection(IEnumerable<CustomPropertyParameter> values) : base(values, _keySelector)
    {
    }

    public static implicit operator CustomPropertyParametersCollection(CustomPropertyParameter[] items)
    {
        return new CustomPropertyParametersCollection(items);
    }
}
