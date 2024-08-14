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
        return type == typeof(DataSourcesMap);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        var dataSources = new DataSourcesMap();

        parser.Consume<MappingStart>();
        while (parser.TryConsume<Scalar>(out var key))
        {
            parser.Consume<MappingStart>();
            var dataSourceType = string.Empty;
            var properties = new Dictionary<string, string>();

            while (parser.TryConsume<Scalar>(out var property))
            {
                var value = parser.Consume<Scalar>().Value;
                properties[property.Value] = value;

                if (property.Value == "Type")
                {
                    dataSourceType = value;
                }
            }

            if (dataSourceType == "DataverseTable")
            {
                var dataSource = new DataverseTableDataSource
                {
                    Type = properties["Type"],
                    EntitySetName = properties["EntitySetName"],
                    LogicalName = properties["LogicalName"],
                    Dataset = properties["Dataset"],
                    InstanceUrl = properties["InstanceUrl"],
                    WebApiVersion = properties["WebApiVersion"],
                    State = properties["State"]
                };
                dataSources.DataSourcesDict[key.Value] = dataSource;
            }
            else if (dataSourceType == "SQLTable")
            {
                var dataSource = new SQLTableDataSource
                {
                    Type = properties["Type"],
                    Database = properties["Database"],
                    Server = properties["Server"],
                    Connection = properties["Connection"]
                };
                dataSources.DataSourcesDict[key.Value] = dataSource;
            }

            parser.Consume<MappingEnd>();
        }
        parser.Consume<MappingEnd>();

        return dataSources;
    }

    // object? IYamlTypeConverter.ReadYaml(IParser parser, Type type)
    // {

    //     var keyValuePairs = new Dictionary<string, string>();
    //     var DataSource = new DataSource();
    //     parser.MoveNext();
    //     while (!parser.Accept<MappingEnd>(out _))
    //     {
    //         var key = parser.Consume<Scalar>().Value;
    //         var value = parser.Consume<Scalar>().Value;
    //         keyValuePairs.Add(key, value);
    //     }

    //     parser.MoveNext();

    //     return DataSource;
    // }

    void IYamlTypeConverter.WriteYaml(IEmitter emitter, object? value, Type type)
    {
        throw new NotImplementedException();
    }
}



