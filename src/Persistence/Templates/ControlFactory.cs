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

    public Control Create(string name, string template, ControlPropertiesCollection? properties = null)
    {
        if (_controlTemplateStore.TryGetTemplateByName(template, out var controlTemplate))
        {
            return new BuiltInControl(name, controlTemplate) { Properties = properties ?? new() };
        }

        return new CustomControl(name, new ControlTemplate(template)) { Properties = properties ?? new() };
    }

    public Control Create(string name, ControlTemplate template, ControlPropertiesCollection? properties = null)
    {
        if (_controlTemplateStore.TryGetControlTypeByName(template.Name, out var controlType))
        {
            var controlObj = Activator.CreateInstance(controlType, name, _controlTemplateStore);
            return controlObj == null
                ? throw new InvalidOperationException($"Failed to create control of type {controlType.Name}.")
                : (Control)controlObj;
        }

        throw new InvalidOperationException($"Unknown template name '{template.Name}'.");
    }

    public App CreateApp(string name, ControlPropertiesCollection? properties = null)
    {
        return new App(name, _controlTemplateStore)
        {
            Properties = properties ?? new(),
            Controls = new Control[] { Create(BuiltInTemplates.Host, BuiltInTemplates.Host) }
        };
    }

    public Screen CreateScreen(string name, ControlPropertiesCollection? properties = null, Control[]? controls = null)
    {
        return new Screen(name, _controlTemplateStore)
        {
            Properties = properties ?? new(),
            Controls = controls ?? Array.Empty<Control>()
        };
    }
}
