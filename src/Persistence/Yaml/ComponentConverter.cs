// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ComponentConverter : ControlConverter, IYamlTypeConverter
{
    public ComponentConverter(IControlFactory controlFactory) : base(controlFactory)
    {
    }

    bool IYamlTypeConverter.Accepts(Type type)
    {
        return type == typeof(ComponentDefinition);
    }

    object? IYamlTypeConverter.ReadYaml(IParser parser, Type type)
    {
        return ReadYaml(parser, type);
    }

    public override object? ReadKey(IParser parser, string key)
    {
        if (key == nameof(ComponentDefinition.CustomProperties))
        {
            using var serializerState = new SerializerState();
            return ValueDeserializer!.DeserializeValue(parser, typeof(List<CustomProperty>), serializerState, ValueDeserializer);
        }

        return base.ReadKey(parser, key);
    }

    void IYamlTypeConverter.WriteYaml(IEmitter emitter, object? value, Type type)
    {
        if (value == null)
            return;

        var component = ((ComponentDefinition)value).BeforeSerialize();
        WriteYamlInternal(emitter, component, type);

        emitter.Emit(nameof(ComponentDefinition.Description), component.Description);

        if (component.AccessAppScope)
        {
            emitter.Emit(new Scalar(nameof(ComponentDefinition.AccessAppScope)));
            ValueSerializer!.SerializeValue(emitter, component.AccessAppScope, typeof(bool));
        }

        if (component.CustomProperties != null && component.CustomProperties.Count > 0)
        {
            emitter.Emit(new Scalar(nameof(ComponentDefinition.CustomProperties)));
            ValueSerializer!.SerializeValue(emitter, component.CustomProperties, typeof(IList<CustomProperty>));
        }

        if (Options.IsControlIdentifiers)
            emitter.Emit(new MappingEnd());
        emitter.Emit(new MappingEnd());
    }
}
