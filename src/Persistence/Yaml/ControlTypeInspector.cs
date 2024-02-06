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
        // For built in controls, we'll use the property name as the template name which will be used to get the template.
        // for example
        // Button:
        // Name: "Button1"
        if (type == typeof(BuiltInControl))
        {
            if (_controlTemplateStore.TryGetTemplateByName(name, out var controlTemplate))
                return new TemplatePropertyDescriptor(name, controlTemplate);
            return _innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);
        }

        // For custom controls, we expect value to be template id.
        // for example
        // Control: http://localhost/#customcontrol
        // Name: My Custom Control
        if (type == typeof(CustomControl) && name == YamlFields.Control)
        {
            return new TemplatePropertyDescriptor(name);
        }

        if (_controlTemplateStore.TryGetName(type, out var templateName))
        {
            // For built in types, we don't need template id.
            // for example
            // App:
            // Name: My Custom Control
            if (name.Equals(templateName))
                return new EmptyPropertyDescriptor(name);
        }

        return _innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);
    }
}
