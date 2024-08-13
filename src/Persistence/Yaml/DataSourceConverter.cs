// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;


namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal sealed class DataSourceConverter : IYamlTypeConverter
{

    public IValueDeserializer? ValueDeserializer { get; set; }

    bool IYamlTypeConverter.Accepts(Type type)
    {
        return type == typeof(DataSource);
    }

    object? IYamlTypeConverter.ReadYaml(IParser parser, Type type)
    {
        var DataSource = new DataSource();
        while (!parser.Accept<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>();
            DataSource.Type = key.Value;
        }

        parser.MoveNext();

        return DataSource;
    }

    void IYamlTypeConverter.WriteYaml(IEmitter emitter, object? value, Type type)
    {
        throw new NotImplementedException();
    }
}



