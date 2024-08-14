// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public class DataSources
{
    [JsonPropertyName("DataSources")]
    public List<DataSource>? Items { get; set; }
}

public class DataSourcesMap
{
    public Dictionary<string, IDataSource> DataSourcesDict { get; set; } = new Dictionary<string, IDataSource>();
}

public interface IDataSource
{
    public string Type { get; set; }
}
public class DataverseTableDataSource : IDataSource
{
    public string Type { get; set; }
    public string EntitySetName { get; set; }
    public string LogicalName { get; set; }
    public string Dataset { get; set; }
    public string InstanceUrl { get; set; }
    public string WebApiVersion { get; set; }
    public string State { get; set; }
}

public class SQLTableDataSource : IDataSource
{
    public string Type { get; set; }
    public string Database { get; set; }
    public string Server { get; set; }
    public string Connection { get; set; }
}

public class DataSource : INamedObject
{
    public string? Type { get; set; }
    public string Name { get; set; }
    public string? EntitySetName { get; set; }
    public string? LogicalName { get; set; }
    public string? Dataset { get; set; }
    public string? InstanceUrl { get; set; }
    public string? WebApiVersion { get; set; }
    public string? State { get; set; }
}

//public class WadlMetadata
//{
//    public required string WadlXml { get; set; }
//}

//public class DataverseTable : DataSource
//{
//    public string? EntitySetName { get; set; }
//    public string? LogicalName { get; set; }
//    public string? Dataset { get; set; }
//    public string? InstanceUrl { get; set; }
//    public string? WebApiVersion { get; set; }
//    public string? State { get; set; }
//}
