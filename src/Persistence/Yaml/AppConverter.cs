// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Core;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal sealed class AppConverter : ControlConverter
{
    public AppConverter(IControlFactory controlFactory) : base(controlFactory)
    {
    }

    public override bool Accepts(Type type)
    {
        return type == typeof(App);
    }

    public override void OnWriteAfterName(IEmitter emitter, Control value)
    {
        if (value == null)
            return;

        var app = (App)value;

        if (app.Screens != null)
        {
            emitter.Emit(new YamlDotNet.Core.Events.Scalar(nameof(App.Screens)));
            ValueSerializer!.SerializeValue(emitter, app.Screens, typeof(List<Screen>));
        }

        base.OnWriteAfterName(emitter, value);
    }

    public override object? ReadKey(IParser parser, string key)
    {
        if (key == nameof(App.Screens))
        {
            using var serializerState = new SerializerState();
            return ValueDeserializer!.DeserializeValue(parser, typeof(List<Screen>), serializerState, ValueDeserializer);
        }

        return base.ReadKey(parser, key);
    }
}
