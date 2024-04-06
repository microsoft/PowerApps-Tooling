// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class AppConverter : ControlConverter, IYamlTypeConverter
{
    public AppConverter(IControlFactory controlFactory) : base(controlFactory)
    {
    }

    bool IYamlTypeConverter.Accepts(Type type)
    {
        return type == typeof(App);
    }

    object? IYamlTypeConverter.ReadYaml(IParser parser, Type type)
    {
        return ReadYaml(parser, type);
    }

    void IYamlTypeConverter.WriteYaml(IEmitter emitter, object? value, Type type)
    {
        if (value == null)
            return;

        var component = ((Component)value).BeforeSerialize<Component>();
        WriteYamlInternal(emitter, component, type);

        if (component.CustomProperties != null && component.CustomProperties.Count > 0)
        {
            emitter.Emit(new YamlDotNet.Core.Events.Scalar(nameof(Component.CustomProperties)));
            ValueSerializer!.SerializeValue(emitter, component.CustomProperties, typeof(CustomPropertiesCollection));
        }

        if (Options.IsControlIdentifiers)
            emitter.Emit(new YamlDotNet.Core.Events.MappingEnd());
        emitter.Emit(new YamlDotNet.Core.Events.MappingEnd());
    }

    public override object? ReadKey(IParser parser, string key)
    {
        if (key == nameof(App.Settings))
        {
            using var serializerState = new SerializerState();
            return ValueDeserializer!.DeserializeValue(parser, typeof(Models.Settings), serializerState, ValueDeserializer);
        }

        if (key == nameof(App.Screens))
        {
            using var serializerState = new SerializerState();
            return ValueDeserializer!.DeserializeValue(parser, typeof(List<Screen>), serializerState, ValueDeserializer);
        }

        return base.ReadKey(parser, key);
    }
}
