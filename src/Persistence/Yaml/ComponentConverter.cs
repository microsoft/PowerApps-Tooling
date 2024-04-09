// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ComponentConverter : ControlConverter, IYamlTypeConverter
{
    public ComponentConverter(IControlFactory controlFactory) : base(controlFactory)
    {
    }

    bool IYamlTypeConverter.Accepts(Type type)
    {
        return type == typeof(Component);
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

        if (!string.IsNullOrWhiteSpace(component.Description))
        {
            emitter.Emit(new Scalar(nameof(Component.Description)));
            ValueSerializer!.SerializeValue(emitter, component.Description, typeof(string));
        }

        if(component.AccessAppScope)
        {
            emitter.Emit(new Scalar(nameof(Component.AccessAppScope)));
            ValueSerializer!.SerializeValue(emitter, component.AccessAppScope, typeof(bool));
        }

        if (component.CustomProperties != null && component.CustomProperties.Count > 0)
        {
            emitter.Emit(new Scalar(nameof(Component.CustomProperties)));
            ValueSerializer!.SerializeValue(emitter, component.CustomProperties, typeof(CustomPropertiesCollection));
        }

        if (Options.IsControlIdentifiers)
            emitter.Emit(new MappingEnd());
        emitter.Emit(new MappingEnd());
    }
}
