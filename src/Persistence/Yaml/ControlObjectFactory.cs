// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class ControlObjectFactory : IObjectFactory
{
    private readonly IObjectFactory _defaultObjectFactory;
    private readonly IControlTemplateStore _controlTemplateStore;
    private readonly IControlFactory _controlFactory;

    public ControlObjectFactory(IControlTemplateStore controlTemplateStore, IControlFactory controlFactory)
    {
        _defaultObjectFactory = new DefaultObjectFactory();
        _controlTemplateStore = controlTemplateStore;
        _controlFactory = controlFactory;
    }

    public object Create(Type type)
    {
        if (_controlTemplateStore.TryGetByType(type, out var controlTemplate))
        {
            // all fields will be overwritten by the deserializer
            return _controlFactory.Create(controlTemplate.InvariantName, controlTemplate);
        }

        // Control is abstract, so we'll try to create a concrete custom control type.
        if (type == typeof(Control))
        {
            return _controlFactory.Create(nameof(CustomControl), nameof(Control));
        }

        return _defaultObjectFactory.Create(type);
    }

    public object? CreatePrimitive(Type type)
    {
        return _defaultObjectFactory.CreatePrimitive(type);
    }

    public void ExecuteOnDeserialized(object value)
    {
        _defaultObjectFactory.ExecuteOnDeserialized(value);
        if (value is Control control)
        {
            RestoreNestedTemplates(control);
        }
    }

    public void ExecuteOnDeserializing(object value)
    {
        _defaultObjectFactory.ExecuteOnDeserializing(value);
    }

    public void ExecuteOnSerialized(object value)
    {
        _defaultObjectFactory.ExecuteOnSerialized(value);
    }

    public void ExecuteOnSerializing(object value)
    {
        _defaultObjectFactory.ExecuteOnSerializing(value);
    }

    public bool GetDictionary(IObjectDescriptor descriptor, out IDictionary? dictionary, out Type[]? genericArguments)
    {
        return _defaultObjectFactory.GetDictionary(descriptor, out dictionary, out genericArguments);
    }

    public Type GetValueType(Type type)
    {
        return _defaultObjectFactory.GetValueType(type);
    }

    internal void RestoreNestedTemplates(Control control)
    {
        if (control.Template == null || control.Template.NestedTemplates == null)
            return;

        foreach (var nestedTemplate in control.Template.NestedTemplates)
        {
            if (!nestedTemplate.AddPropertiesToParent)
                continue;

            var nestedControl = _controlFactory.Create(Guid.NewGuid().ToString(), nestedTemplate);
            control.Children ??= new List<Control>();
            control.Children.Add(nestedControl);

            // Move properties from parent to nested template
            for (var i = 0; i < control.Properties.Count; i++)
            {
                var property = control.Properties.ElementAt(i);
                if (!nestedTemplate.InputProperties.ContainsKey(property.Key))
                    continue;
                nestedControl.Properties.Add(property.Key, property.Value);
                control.Properties.Remove(property.Key);
                i--;
            }
        }
    }
}
