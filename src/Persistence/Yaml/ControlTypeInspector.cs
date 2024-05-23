// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal sealed class ControlTypeInspector(
    ITypeInspector innerTypeInspector,
    IControlTemplateStore controlTemplateStore)
    : ITypeInspector
{
    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = innerTypeInspector.GetProperties(type, container);
        if (controlTemplateStore.Contains(type))
            return properties.Where(p => !p.Name.Equals(YamlFields.Control, StringComparison.Ordinal));

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
            // for example
            // Control: Button
            // Name: "Button1"
            if (name == YamlFields.Control)
                return new TemplatePropertyDescriptor(controlTemplateStore);

            if (controlTemplateStore.TryGetTemplateByName(name, out var controlTemplate))
                return new TemplatePropertyDescriptor(controlTemplateStore, controlTemplate);
            return innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);
        }

        // For custom controls, we expect value to be template id.
        // for example
        // Control: http://localhost/#customcontrol
        // Name: My Custom Control
        if (type == typeof(CustomControl) && name == YamlFields.Control)
        {
            return new TemplatePropertyDescriptor(controlTemplateStore);
        }

        if (controlTemplateStore.TryGetName(type, out var templateName))
        {
            // For built in types, we don't need template id.
            // for example
            // App:
            // Name: My Custom Control
            if (name.Equals(templateName, StringComparison.Ordinal))
                return new EmptyPropertyDescriptor(name);
        }

        // For controls all properties have to match the list of expected properties.
        // This improves error reporting via setting ignoreUnmatched to false.
        if (type == typeof(Control))
            return innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched: false);

        return innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);
    }
}
