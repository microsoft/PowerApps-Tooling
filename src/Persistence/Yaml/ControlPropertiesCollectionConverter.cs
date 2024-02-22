// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class ControlPropertiesCollectionConverter : IYamlTypeConverter
{
    private NullNodeDeserializer _nullNodeDeserializer = new NullNodeDeserializer();
    private Scalar NullScalar = new Scalar("tag:yaml.org,2002:null", string.Empty);

    public bool IsTextFirst { get; set; } = true;

    public bool Accepts(Type type)
    {
        return type == typeof(ControlPropertiesCollection);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        var collection = new ControlPropertiesCollection();

        parser.MoveNext();

        while (!parser.Accept<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>();
            var scalarValue = parser.Consume<Scalar>();
            var value = scalarValue.Value;
            if (scalarValue.Value == null || _nullNodeDeserializer.Deserialize(parser, typeof(object), null!, out _))
                value = null;

            if (IsTextFirst)
                collection.Add(key.Value, ControlPropertyValue.FromTextFirstString(value));
            else
                collection.Add(key.Value, new ControlPropertyValue() { Value = value });
        }

        parser.MoveNext();

        return collection;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var collection = (ControlPropertiesCollection)value!;

        emitter.Emit(new MappingStart());

        var sortedKeys = collection.Keys.OrderBy(x => x, StringComparer.Ordinal);

        foreach (var key in sortedKeys)
        {
            emitter.Emit(new Scalar(key));

            var property = collection[key];
            var propertyValue = property.Value;
            if (IsTextFirst)
            {
                if (propertyValue == null)
                {
                    emitter.Emit(NullScalar);
                    continue;
                }

                // String values should be quoted and anything else is formula starting with '='.
                if (1 < propertyValue.Length && propertyValue.StartsWith('\"') && propertyValue.EndsWith('\"'))
                    propertyValue = propertyValue[1..(propertyValue.Length - 1)];
                else
                    propertyValue = $"={propertyValue!}";
            }

            if (propertyValue != null && (propertyValue.Contains(" #") || propertyValue.Contains(": ") || propertyValue.Contains('\"')))
                emitter.Emit(new Scalar(null, null, propertyValue, ScalarStyle.Literal, true, false));
            else
                emitter.Emit(new Scalar(propertyValue!));
        }

        emitter.Emit(new MappingEnd());
    }
}
