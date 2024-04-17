// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ComponentConverter : ControlConverter
{
    public ComponentConverter(IControlFactory controlFactory) : base(controlFactory)
    {
    }

    public override bool Accepts(Type type)
    {
        return type == typeof(ComponentDefinition) || type == typeof(ComponentInstance);
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

    public override string GetControlTemplateName(Control control)
    {
        return BuiltInTemplates.Component.Name;
    }

    public override void OnWriteAfterName(IEmitter emitter, Control control)
    {
        // Nothing special to write for ComponentInstance
        if (control is ComponentInstance componentInstance)
        {
            emitter.Emit(nameof(ComponentInstance.ComponentName), componentInstance.ComponentName);
            emitter.Emit(nameof(ComponentInstance.ComponentLibraryUniqueName), componentInstance.ComponentLibraryUniqueName);
            base.OnWriteAfterName(emitter, control);
            return;
        }

        var component = (ComponentDefinition)control;

        emitter.Emit(nameof(ComponentDefinition.Description), component.Description);
        base.OnWriteAfterName(emitter, control);

        if (component.Type != ComponentType.Canvas)
        {
            emitter.Emit(nameof(ComponentDefinition.Type), component.Type.ToString());
        }
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
    }
}
