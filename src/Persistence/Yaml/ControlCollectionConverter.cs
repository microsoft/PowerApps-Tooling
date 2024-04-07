// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal class ControlCollectionConverter : IYamlTypeConverter
{
    public ControlCollectionConverter()
    {
    }

    [SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Explicitly setting to false for clarity")]
    public bool IsTextFirst { get; set; } = false;

    public IValueDeserializer? ValueDeserializer { get; set; }

    public bool Accepts(Type type)
    {
        return
            type == typeof(List<object>) || type.IsSubclassOf(typeof(List<object>)) ||
            type == typeof(IList<object>) || type.IsSubclassOf(typeof(IList<object>)) ||
            type == typeof(List<Control>) || type.IsSubclassOf(typeof(List<Control>)) ||
            type == typeof(IList<Control>) || type.IsSubclassOf(typeof(IList<Control>));
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        if (parser.Current is not SequenceStart)
            throw new YamlException(parser.Current!.Start, parser.Current.End, $"Expected sequence start but got {parser.Current.GetType().Name}");

        if (!parser.MoveNext())
            throw new YamlException(parser.Current.Start, parser.Current.End, "Expected start of control");

        var controls = new List<Control>();
        while (!parser.Accept<SequenceEnd>(out _))
        {
            using var serializerState = new SerializerState();
            var controlObj = ValueDeserializer!.DeserializeValue(parser, typeof(Control), serializerState, ValueDeserializer);
            if (controlObj == null || controlObj is not Control)
                throw new YamlException(parser.Current.Start, parser.Current.End, "Expected control object");
            controls.Add((Control)controlObj);
        }

        parser.MoveNext();

        return controls;
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
