// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PAModel.Schemas.adhoc
{
    // connections are environment specific. 
    // Connections are the credentials. 
    class ConnectionJson
    {
        public string id { get; set; } // a guid

        // Names that match to DataSources
        // Multiple data sources may use a single connection 
        // For example, 1 sql connection may allow multiple tables (datasources).
        public string[] dataSources { get; set; }


        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}
