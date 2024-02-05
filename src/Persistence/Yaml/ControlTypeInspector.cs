// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ControlTypeInspector : ITypeInspector
{
    private readonly ITypeInspector _innerTypeInspector;
    private readonly IControlTemplateStore _controlTemplateStore;

    public ControlTypeInspector(ITypeInspector innerTypeInspector, IControlTemplateStore controlTemplateStore)
    {
        _innerTypeInspector = innerTypeInspector;
        _controlTemplateStore = controlTemplateStore;
    }

    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = _innerTypeInspector.GetProperties(type, container);
        if (_controlTemplateStore.Contains(type))
            return properties.Where(p => !p.Name.Equals(YamlFields.Control));

        return properties;
    }

    public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched)
    {
        // For any type that isn't a built in, we'll use the default inspector.
        if (!_controlTemplateStore.TryGetName(type, out var shortName))
        {
            if (type == typeof(BuiltInControl))
            {
                if (_controlTemplateStore.TryGetTemplateByName(name, out var controlTemplate))
                    return new TemplatePropertyDescriptor(name, controlTemplate);
            }
            else if (type == typeof(CustomControl) && name == YamlFields.Control)
                return new TemplatePropertyDescriptor(name);

            return _innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);
        }

        if (!name.Equals(shortName))
            return _innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);

        return new EmptyPropertyDescriptor(name);
    }
}
