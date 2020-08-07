//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AppMagic.Authoring.Persistence
{

    // $$$ Same as DataSourceEntry?
    // Just the set of properties we write publicly.
    public class DataSourceModel
    {
        // Name is what shows up in formulas. 
        public string Name { get; set; } // "MSNWeather",

        // Uniquely identfies connector type. 
        public string Type { get; set; } 
        public string ApiId { get; set; } // "/providers/microsoft.powerapps/apis/shared_msnweather"

        // For Sharepoint:
        public string DatasetName { get; set; } // "https://microsoft.sharepoint.com/teams/Test85a"
        public string TableName { get; set; } // a guid 
                                              // DataEntityMetadataJson -- The big one!!!!

        public string GetSharepointListName()
        {
            var phrase = ".sharepoint.com/teams/";
            int i = this.DatasetName.IndexOf(phrase);
            if (i <=0)
            {
                throw new InvalidOperationException($"Unrecognized dataset: {this.DatasetName}");
            }
            return this.DatasetName.Substring(i + phrase.Length);
        }
/*
    }

    // 
    public class DataSourceEntry : DataSourceModel 
    {*/
        // Key is guid, value is Json-encoded metadata. 
        public IDictionary<string, string> DataEntityMetadataJson { get; set; }
        public string TableDefinition { get; set; } // used for 

        // Type: very polymorphic. 

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    public class DataSourceEntry : DataSourceModel { }

    public class DataSourcesJson
    {
        public DataSourceEntry[] DataSources { get; set; }

        // Requires everything to have this...
        // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to#handle-overflow-json
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}