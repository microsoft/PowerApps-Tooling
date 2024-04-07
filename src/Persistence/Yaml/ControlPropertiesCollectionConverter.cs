// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class ControlPropertiesCollectionConverter : IYamlTypeConverter
{
    private readonly NullNodeDeserializer _nullNodeDeserializer = new();
    private readonly Scalar NullScalar = new("tag:yaml.org,2002:null", string.Empty);

    public required YamlSerializationOptions Options { get; set; }

    public bool Accepts(Type type)
    {
        return type == typeof(ControlPropertiesCollection);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        _ = parser ?? throw new ArgumentNullException(nameof(parser));

        var collection = new ControlPropertiesCollection();

        parser.MoveNext();

        while (!parser.Accept<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>();
            string? value = null;
            if (!_nullNodeDeserializer.Deserialize(parser, typeof(object), null!, out _))
                value = parser.Consume<Scalar>().Value;

            if (Options.IsTextFirst)
                collection.Add(key.Value, ControlProperty.FromTextFirstString(key.Value, value));
            else
            {
                collection.Add(key.Value, value?.TrimStart('='));
            }
        }

        parser.MoveNext();

        return collection;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var collection = (ControlPropertiesCollection)value!;

        emitter.Emit(new MappingStart());

        var sortedKeys = collection.Values.OrderBy(p => p, Comparer<ControlProperty>.Default);

        foreach (var key in sortedKeys)
        {
            emitter.Emit(new Scalar(key.Name));

            var property = collection[key.Name];
            var propertyValue = property.Value;
            if (Options.IsTextFirst)
            {
                if (propertyValue == null)
                {
                    emitter.Emit(NullScalar);
                    continue;
                }

                // String values should be quoted and anything else is formula starting with '='.
                if (1 < propertyValue.Length && propertyValue.StartsWith('\"') && propertyValue.EndsWith('\"') && !propertyValue.StartsWith("\"="))
                {
                    propertyValue = propertyValue[1..(propertyValue.Length - 1)];
                }
                else
                    propertyValue = $"={propertyValue!}";
            }
            else
            {
                if (propertyValue == null)
                {
                    emitter.Emit(NullScalar);
                    continue;
                }

                propertyValue = propertyValue.StartsWith('=') ? propertyValue : $"={propertyValue}";
            }

            var scalarStyle = propertyValue.DetermineScalarStyleForProperty();
            emitter.Emit(new Scalar(null, null, propertyValue, scalarStyle, true, false));
        }

        emitter.Emit(new MappingEnd());
    }
}
