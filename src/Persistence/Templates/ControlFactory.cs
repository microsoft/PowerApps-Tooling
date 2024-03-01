// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public class ControlFactory : IControlFactory
{
    private readonly IControlTemplateStore _controlTemplateStore;

    public ControlFactory(IControlTemplateStore controlTemplateStore)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
    }

    public Control Create(string name, string template, ControlPropertiesCollection? properties = null, Control[]? children = null)
    {
        if (_controlTemplateStore.TryGetTemplateByName(template, out var controlTemplate))
        {
            return new BuiltInControl(name, controlTemplate)
            {
                Properties = properties ?? new(),
                Children = children ?? Array.Empty<Control>()
            };
        }

        return new CustomControl(name, new ControlTemplate(template))
        {
            Properties = properties ?? new(),
            Children = children ?? Array.Empty<Control>()
        };
    }

    public Control Create(string name, ControlTemplate template, ControlPropertiesCollection? properties = null)
    {
        if (_controlTemplateStore.TryGetControlTypeByName(template.Name, out var controlType))
        {
            var instance = Activator.CreateInstance(controlType, name, _controlTemplateStore);
            if (instance is not Control control)
                throw new InvalidOperationException($"Failed to create control of type {controlType.Name}.");

            if (properties is not null)
                foreach (var prop in properties)
                    control.Properties.Add(prop.Key, prop.Value);

            return control;
        }

        throw new InvalidOperationException($"Unknown template name '{template.Name}'.");
    }

    public App CreateApp(string name, ControlPropertiesCollection? properties = null)
    {
        return new App(name, _controlTemplateStore)
        {
            Properties = properties ?? new(),
            Children = new Control[] { Create("Host", BuiltInTemplates.Host) }
        };
    }

    public Screen CreateScreen(string name, ControlPropertiesCollection? properties = null, Control[]? children = null)
    {
        return new Screen(name, _controlTemplateStore)
        {
            Properties = properties ?? new(),
            Children = children ?? Array.Empty<Control>()
        };
    }
}
