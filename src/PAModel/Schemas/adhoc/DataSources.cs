// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AppMagic.Authoring.Persistence;

namespace Microsoft.PowerPlatform.Formulas.Tools;

internal class DataSourceModel
{
    public const string DataComponentType = "DataComponent";

    // Name is what shows up in formulas. 
    public string Name { get; set; } // "MSNWeather",

    // Uniquely identifies connector type. 
    public string Type { get; set; }
    public string ApiId { get; set; } // "/providers/microsoft.powerapps/apis/shared_msnweather"

    // For SharePoint:
    public string DatasetName { get; set; } // "https://microsoft.sharepoint.com/teams/Test85a"
    public string TableName { get; set; } // a guid 

    public string RelatedEntityName { get; set; }

    // Get a unique filename. Can include \ directory character to place in subdirs. 
    public string GetUniqueName()
    {
        if (IsDataComponent)
        {
            return $@"datacomponent\{Name}";
        }

        if (RelatedEntityName != null)
        {
            return $@"{RelatedEntityName}\{Name}";
        }

        return Name;
    }

    // DataEntityMetadataJson -- The big one!!!!

    // Used for Data components. (Type=="DataComponent")
    // Key back to component name. 
    // This field is added to aide in merging DataComponent data sources into regular data sources.
    // We really should point ot the ComponentInstance, not template. 
    // public string DataComponentTemplate { get; set; } // "Component1"
    public DataComponentSourcesJson.Entry DataComponentDetails { get; set; }

    // Don't serialize. 
    internal bool IsDataComponent => DataComponentDetails != null;

    // Was the environment guid removed from the view name?
    // This allows for switching environments to just switch the pkg folder
    public bool? TrimmedViewName { get; set; } = null;

    // Key is guid, value is Json-encoded metadata. 
    public IDictionary<string, string> DataEntityMetadataJson { get; set; }
    public string TableDefinition { get; set; }
    public WadlDefinition WadlMetadata { get; set; }
    // Type: very polymorphic. 

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}

internal class WadlDefinition
{
    public string WadlXml { get; set; }
    public string SwaggerJson { get; set; }
}


internal class DataSourceDefinition
{
    public string DatasetName { get; set; }
    public string EntityName { get; set; }
    public string TableName { get; set; }
    public string InstanceUrl { get; set; }
    public DataSourceTableDefinition TableDefinition { get; set; }
    // Key is guid, value is Json-encoded metadata. 
    public IDictionary<string, string> DataEntityMetadataJson { get; set; }
    public LocalDatabaseReferenceDataSource LocalReferenceDSJson { get; set; }

    /// <summary>
    /// Tracks the unused data sources for the dataset this data source definition belongs too
    /// It is IReadOnlyDictionary to avoid mutations as all definitions with same DatasetName share the same instance
    /// This is done to avoid copying which could be expensive.
    /// Read Only makes the shared instance virtually immutable.
    /// </summary>
    public IReadOnlyDictionary<string, LocalDatabaseReferenceDataSource> UnusedDataSources { get; set; } = null;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}

internal class DataSourceTableDefinition
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}

internal class SwaggerDefinition
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}

internal class DataSourceEntry : DataSourceModel
{

}

internal class DataSourcesJson
{
    // Order here is random on server. 
    public DataSourceEntry[] DataSources { get; set; }

    // Requires everything to have this...
    // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to#handle-overflow-json
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}
