// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core.Events;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class ControlPropertiesCollectionConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(ControlPropertiesCollection);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        var tmpDict = new Dictionary<string, ControlPropertyValue>();

        parser.MoveNext();

        while (!parser.Accept<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>();
            var value = parser.Consume<Scalar>();

            tmpDict.Add(key.Value, new ControlPropertyValue() { Value = value.Value });
        }

        parser.MoveNext();

        return new ControlPropertiesCollection(tmpDict);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var collection = (ControlPropertiesCollection)value!;

        emitter.Emit(new MappingStart());

        var sortedKeys = collection.Keys.OrderBy(x => x);

        foreach (var key in sortedKeys)
        {
            emitter.Emit(new Scalar(key));

#pragma warning disable CS8604 // Possible null reference argument, but valid in YAML.
            var property = collection[key];
            emitter.Emit(new Scalar(property.Value));
#pragma warning restore CS8604 // Possible null reference argument.
        }

        emitter.Emit(new MappingEnd());
    }
}
