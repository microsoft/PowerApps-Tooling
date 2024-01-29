// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ControlTypeInspector : ITypeInspector
{
    private readonly ITypeInspector _innerTypeInspector;

    public ControlTypeInspector(ITypeInspector innerTypeInspector)
    {
        _innerTypeInspector = innerTypeInspector;
    }

    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = _innerTypeInspector.GetProperties(type, container);
        if (BuiltInTemplates.TypeToShortName.TryGetValue(type, out var shortName))
            return properties.Where(p => !p.Name.Equals(nameof(Control)));

        return properties;
    }

    public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched)
    {
        // For any type that isn't a built in, we'll use the default inspector.
        if (!BuiltInTemplates.TypeToShortName.TryGetValue(type, out var shortName))
            return _innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);

        if (!name.Equals(shortName))
            return _innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);

        return new EmptyPropertyDescriptor(name);
    }
}
