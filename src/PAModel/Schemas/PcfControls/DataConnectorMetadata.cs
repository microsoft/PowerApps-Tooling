using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    public struct DataConnectorMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Required { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
