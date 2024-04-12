// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ControlConverter : IYamlTypeConverter
{
    private readonly NullNodeDeserializer _nullNodeDeserializer = new();
    protected IControlFactory _controlFactory;

    public ControlConverter(IControlFactory controlFactory)
    {
        _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));
    }

    public required YamlSerializationOptions Options { get; set; }

    public IValueDeserializer? ValueDeserializer { get; set; }

    public IValueSerializer? ValueSerializer { get; set; }

    public bool Accepts(Type type)
    {
        return type == typeof(Control) || type.IsSubclassOf(typeof(Control));
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        ReadControlDefinitonHeader(parser, out var controlName, out var templateName);

        var controlDefinition = new Dictionary<string, object?>();
        while (!parser.Accept<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>();
            if (controlDefinition.TryGetValue(key.Value, out var _))
                throw new YamlException(key.Start, key.End, $"Duplicate control property '{key.Value}' in control '{controlName}'");

            // Check if the value is null
            object? value = null;
            if (_nullNodeDeserializer.Deserialize(parser, typeof(object), null!, out _))
            {
                controlDefinition.Add(key.Value, value);
                continue;
            }

            // Consume known definition
            if (key.Value == nameof(Control.Properties))
            {
                using var serializerState = new SerializerState();
                value = ValueDeserializer!.DeserializeValue(parser, typeof(ControlPropertiesCollection), serializerState, ValueDeserializer);
            }
            else if (key.Value == nameof(Control.Children))
            {
                using var serializerState = new SerializerState();
                value = ValueDeserializer!.DeserializeValue(parser, typeof(List<Control>), serializerState, ValueDeserializer);
            }
            else if (key.Value == nameof(ComponentDefinition.CustomProperties))
            {
                using var serializerState = new SerializerState();
                value = ValueDeserializer!.DeserializeValue(parser, typeof(List<CustomProperty>), serializerState, ValueDeserializer);
            }
            else
            {
                if (parser.Current is Scalar)
                {
                    value = parser.Consume<Scalar>().Value;
                    if (Options.IsControlIdentifiers)
                    {
                        if (key.Value == nameof(Control))
                            templateName = (string)value;
                    }
                    else
                    {
                        if (key.Value == nameof(Control.Name))
                            controlName = (string)value;
                    }
                }
                else
                    value = ReadKey(parser, key.Value);
            }

            controlDefinition.Add(key.Value, value);
        }

        if (Options.IsControlIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                throw new YamlException(parser.Current!.Start, parser.Current.End, $"Control '{controlName}' doesn't have template name");
        }

        parser.MoveNext();
        if (Options.IsControlIdentifiers)
            parser.MoveNext();

        var control = _controlFactory.Create(string.IsNullOrWhiteSpace(controlName) ? templateName : controlName, templateName, controlDefinition);
        return control.AfterDeserialize(_controlFactory);
    }

    public void ReadControlDefinitonHeader(IParser parser, out string controlName, out string templateName)
    {
        if (!parser.MoveNext())
            throw new YamlException(parser.Current!.Start, parser.Current.End, "Expected start of control definition");

        if (parser.Current is not Scalar scalar)
            throw new YamlException(parser.Current!.Start, parser.Current.End, $"Expected control but got {parser.Current.GetType().Name}");

        controlName = string.Empty;
        templateName = string.Empty;
        if (Options.IsControlIdentifiers)
        {
            controlName = scalar.Value;
        }
        else
        {
            if (scalar.Value == nameof(Control))
            {
                parser.Consume<Scalar>();
                if (parser.Current is not Scalar templateScalar)
                    throw new YamlException(parser.Current.Start, parser.Current.End, $"Expected control template name or id but got {parser.Current.GetType().Name}");
                templateName = templateScalar.Value;
            }
            else
            {
                templateName = scalar.Value;
                // Consume empty scalar after template name
                parser.Consume<Scalar>();
            }
        }

        if (!parser.MoveNext())
            throw new YamlException(parser.Current.Start, parser.Current.End, "Expected begining of control definition");

        if (Options.IsControlIdentifiers)
        {
            if (parser.Current is not MappingStart)
                throw new YamlException(parser.Current.Start, parser.Current.End, $"Expected control definition but got {parser.Current.GetType().Name}");

            if (!parser.MoveNext())
                throw new YamlException(parser.Current.Start, parser.Current.End, "Expected start of control definition");
        }
    }

    public virtual object? ReadKey(IParser parser, string key)
    {
        if (parser.Current is MappingStart)
        {
            using var serializerState = new SerializerState();
            return ValueDeserializer!.DeserializeValue(parser, typeof(object), serializerState, ValueDeserializer);
        }

        return null;
    }

    protected void WriteYamlInternal(IEmitter emitter, Control control, Type type)
    {
        if (Options.IsControlIdentifiers)
        {
            emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));
            emitter.Emit(new Scalar(null, null, control.Name, control.Name.DetermineScalarStyleForProperty(), true, false));
            emitter.Emit(new MappingStart());
            emitter.Emit(nameof(Control), control.Template.DisplayName);
        }
        else
        {
            emitter.Emit(new MappingStart());
            emitter.Emit(nameof(Control), control.Template.DisplayName);
            emitter.Emit(nameof(Control.Name), control.Name);
        }

        emitter.Emit(nameof(Control.Variant), control.Variant);
        emitter.Emit(nameof(Control.Layout), control.Layout);

        if (control.Properties != null && control.Properties.Count > 0)
        {
            emitter.Emit(new Scalar(nameof(Control.Properties)));
            ValueSerializer!.SerializeValue(emitter, control.Properties, typeof(ControlPropertiesCollection));
        }

        if (control.Children != null && control.Children.Count > 0)
        {
            emitter.Emit(new Scalar(nameof(Control.Children)));
            ValueSerializer!.SerializeValue(emitter, control.Children, typeof(IList<Control>));
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        if (value == null)
            return;

        var control = ((Control)value).BeforeSerialize<Control>();
        WriteYamlInternal(emitter, control, type);

        if (Options.IsControlIdentifiers)
            emitter.Emit(new MappingEnd());
        emitter.Emit(new MappingEnd());
    }
}
