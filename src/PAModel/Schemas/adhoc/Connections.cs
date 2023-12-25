// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools;

// connections are environment specific. 
// Connections are the credentials. 
internal class ConnectionJson
{
    public string id { get; set; } // a guid

    // Names that match to DataSources
    // Multiple data sources may use a single connection 
    // For example, 1 sql connection may allow multiple tables (datasources).
    public string[] dataSources { get; set; }


    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}
