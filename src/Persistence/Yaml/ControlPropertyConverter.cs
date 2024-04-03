// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core.Events;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ControlPropertyConverter : IYamlTypeConverter
{
    static internal readonly char[] LineTerminators = new char[] { '\r', '\n', '\x85', '\x2028', '\x2029' };

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
        var style = DetermineScalarStyleForProperty(property.Value);

#pragma warning disable CS8604 // Possible null reference with property value, but it's legal in YAML.
        emitter.Emit(new Scalar(null, null, property.Value, style, true, false));
#pragma warning restore CS8604 
    }

    internal static ScalarStyle DetermineScalarStyleForProperty(string? property)
    {
        if (property == null)
        {
            return ScalarStyle.Plain;
        }
        else if (property.Any(c => LineTerminators.Contains(c)))
        {
            return ScalarStyle.Literal;
        }
        else if (property.Contains(" #") || property.Contains(": "))
        {
            // These sequences break YAML parsing when outside of a literal block
            return ScalarStyle.Literal;
        }
        else
        {
            return ScalarStyle.Plain;
        }
    }
}
