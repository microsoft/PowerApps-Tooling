// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class TemplatePropertyDescriptor : IPropertyDescriptor
{
    private readonly IControlTemplateStore _controlTemplateStore;
    private readonly ControlTemplate? _controlTemplate;

    public TemplatePropertyDescriptor(IControlTemplateStore controlTemplateStore, ControlTemplate? controlTemplate = null)
    {
        _controlTemplateStore = controlTemplateStore;
        _controlTemplate = controlTemplate;
    }

    public string Name { get; } = nameof(Control.Template);

    public Type Type => typeof(object);

    public Type? TypeOverride { get; set; }

    public int Order { get; set; }

    public ScalarStyle ScalarStyle { get; set; }

    public bool CanWrite => true;

    public void Write(object? target, object? value)
    {
        if (target == null)
            return;

        var templateProperty = target.GetType().GetProperty(nameof(Control.Template)) ?? throw new InvalidOperationException($"Target does not have a {nameof(Control.Template)} property.");
        if (_controlTemplate == null)
        {
            if (value is not string)
                throw new InvalidOperationException("Value is not a string.");

            if (_controlTemplateStore.TryGetByIdOrName((string)value, out var controlTemplate))
                templateProperty.SetValue(target, controlTemplate);
            else
                templateProperty.SetValue(target, new ControlTemplate((string)value));
        }
        else
            templateProperty.SetValue(target, _controlTemplate);
    }

    public T? GetCustomAttribute<T>() where T : Attribute
    {
        return null;
    }

    public IObjectDescriptor Read(object? target)
    {
        return new ObjectDescriptor(target, typeof(object), typeof(object));
    }
}
