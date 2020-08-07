//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    // Writes to \references\DataComponentSources.json
    public class DataComponentSourcesJson
    {
        public class Entry
        {
            // The template guid
            public string AssociatedDataComponentTemplate { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public Entry[] DataSources { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    // Writes to \References\DataComponentTemplates.json
    public class DataComponentTemplatesJson
    {
        public class Entry
        {
            // Matches to DataComponentsMetadataJson.TemplateName
            public string Name { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public Entry[] ComponentTemplates { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
    



    // Writes to \ComponentsMetadata.json
    public class DataComponentsMetadataJson
    {
        public class Entry
        {
            public string Name { get; set;  } // "Component1";
            public string TemplateName { get; set; } // "a70e51d571ae4649a16b8bf1622ffdac";

            public string Description { get; set; }
            public bool AllowCustomization { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public Entry[] Components { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}