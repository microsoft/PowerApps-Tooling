// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal sealed class NamedObjectTypeInspector(ITypeInspector innerTypeInspector) : ITypeInspector
{
    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = innerTypeInspector.GetProperties(type, container);
        if (typeof(INamedObject).IsAssignableFrom(type))
            return properties.Where(p => !p.Name.Equals(nameof(INamedObject.Name), StringComparison.Ordinal));

        return properties;
    }

    public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched)
    {
        return innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);
    }
}
