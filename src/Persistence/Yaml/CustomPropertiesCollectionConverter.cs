// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class CustomPropertiesCollectionConverter : IYamlTypeConverter
{
    public required YamlSerializationOptions Options { get; set; }

    public IValueDeserializer? ValueDeserializer { get; set; }

    public IValueSerializer? ValueSerializer { get; set; }

    public bool Accepts(Type type)
    {
        return type == typeof(CustomPropertiesCollection);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        _ = parser ?? throw new ArgumentNullException(nameof(parser));

        var collection = new CustomPropertiesCollection();

        parser.MoveNext();

        while (!parser.Accept<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>();
            var value = ValueDeserializer!.DeserializeValue(parser, typeof(CustomProperty), null!, ValueDeserializer) as CustomProperty;
            if (value == null)
                throw new YamlException(parser.Current!.Start, parser.Current.End, $"Expected custom property '{key.Value}' value");

            if (value.Parameters != null)
            {
                foreach (var paramKv in value.Parameters)
                {
                    paramKv.Value.Name = paramKv.Key;
                }
            }

            value.Name = key.Value;
            collection.Add(key.Value, value);
        }

        parser.MoveNext();

        return collection;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var collection = (CustomPropertiesCollection)value!;

        emitter.Emit(new MappingStart());

        foreach (var key in collection)
        {
            emitter.Emit(new Scalar(key.Key));
            ValueSerializer!.SerializeValue(emitter, key.Value, typeof(CustomProperty));
        }

        emitter.Emit(new MappingEnd());
    }
}
