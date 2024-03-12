// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
public class ControlConverter : IYamlTypeConverter
{
    private readonly IControlTemplateStore _controlTemplateStore;
    private readonly ControlObjectFactory? _objectFactory;

    public ControlConverter(IControlTemplateStore controlTemplateStore, ControlObjectFactory? objectFactory = null)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
        _objectFactory = objectFactory;
    }

    public bool Accepts(Type type)
    {
        return type.IsAssignableTo(typeof(Control));
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        _ = _objectFactory ?? throw new ArgumentNullException(nameof(_objectFactory));

        var children = new List<Control>();
        var properties = new ControlPropertiesCollection();

        parser.Consume<MappingStart>();
        var name = parser.Consume<Scalar>().Value;

        // Indent for the rest of the control
        parser.Consume<MappingStart>();

        // Top level properties - TODO - currently assuming ony one, control type, already read above
        parser.Consume<Scalar>(); // Control
        var controlTemplate = parser.Consume<Scalar>().Value;
        var template = _controlTemplateStore.TryGetByIdOrName(controlTemplate, out var templateValue)
            ? templateValue
            : new ControlTemplate(controlTemplate);
        var controlType = DiscriminateControlType(controlTemplate);

        // Properties bag
        if (parser.Accept<Scalar>(out var props) && props?.Value == "Properties")
        {
            parser.Consume<Scalar>();
            var propsConverter = new ControlPropertiesCollectionConverter();
            properties = (ControlPropertiesCollection)propsConverter.ReadYaml(parser, typeof(ControlPropertiesCollection));
        }

        // Children
        if (parser.Accept<Scalar>(out var childrenNode) && childrenNode?.Value == "Children")
        {
            parser.Consume<Scalar>();
            parser.Consume<SequenceStart>();
            while (!parser.Accept<SequenceEnd>(out _))
            {
                children.Add((Control)ReadYaml(parser, typeof(Control))!);
            }
            parser.Consume<SequenceEnd>();
        }

        parser.Consume<MappingEnd>();
        parser.Consume<MappingEnd>();

        var initalControl = controlType?.Name == "Screen" || controlType?.Name == "App"
            ? (Control)Activator.CreateInstance(controlType!, name, _controlTemplateStore)!
            : (Control)Activator.CreateInstance(controlType!, name, template!)!;

        var control = initalControl with { Properties = properties, Children = children.Count > 0 ? children : null };
        control.AfterDeserialize();
        _objectFactory.RestoreNestedTemplates(control);

        return control;
    }

    private Type DiscriminateControlType(string controlValue)
    {
        if (_controlTemplateStore.TryGetByIdOrName(controlValue.Trim(), out var controlTemplate))
        {
            // It can be one of the built-in types.
            if (_controlTemplateStore.TryGetControlTypeByName(controlTemplate.Name, out var controlType))
            {
                return controlType;
            }
            return typeof(BuiltInControl);
        }

        // If we don't have this template, we'll use the custom control type.
        return typeof(CustomControl);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var control = (Control)value!;

        control.BeforeSerialize();

        emitter.Emit(new MappingStart());

        // Emit the name as a left-side value
        emitter.Emit(new Scalar(control.Name));

        emitter.Emit(new MappingStart());

        emitter.Emit(new Scalar("Control"));
        emitter.Emit(new Scalar(control.Template.DisplayName));

        if (control.Properties != null && control.Properties.Count > 0)
        {
            emitter.Emit(new Scalar("Properties"));

            new ControlPropertiesCollectionConverter()
                .WriteYaml(emitter, control.Properties, typeof(ControlPropertiesCollection));
        }

        if (control.Children != null && control.Children.Count > 0)
        {
            emitter.Emit(new Scalar("Children"));

            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
            foreach (var child in control.Children)
            {
                WriteYaml(emitter, child, type);
            }
            emitter.Emit(new SequenceEnd());
        }

        emitter.Emit(new MappingEnd());
        emitter.Emit(new MappingEnd());
    }
}
