// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core.Events;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal sealed class ControlPropertyConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(ControlProperty);
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        throw new NotImplementedException();
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var property = (ControlProperty)value!;
        var style = property.Value.DetermineScalarStyleForProperty();

#pragma warning disable CS8604 // Possible null reference with property value, but it's legal in YAML.
        emitter.Emit(new Scalar(null, null, property.Value, style, true, false));
#pragma warning restore CS8604 
    }
}
