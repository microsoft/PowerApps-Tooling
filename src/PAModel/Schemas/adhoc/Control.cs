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
    // $$$ todo - get real definition
    class ControlInfoJson
    {
        public class RuleEntry
        {
            public string Property { get; set; }

            // The PA formulas!
            public string InvariantScript { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }
        
        public class Template
        {
            public const string DataComponentId = "http://microsoft.com/appmagic/DataComponent";
            public string Id { get; set; }

            // Very important for data components.
            public string Name { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public class Item
        {
            public string Name { get; set; } // Control name 
            public string ControlUniqueId { get; set; }
            
            public Template Template { get; set; }
            public RuleEntry[] Rules { get; set; }

            public Item[] Children { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public Item TopParent { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}