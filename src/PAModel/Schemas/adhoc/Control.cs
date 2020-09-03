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

            public string NameMap { get; set; }

            //[JsonExtensionData]
            //public Dictionary<string, JsonElement> ExtensionData { get; set; }

            // Duplicate, present in template as well
            public string Category { get; set; }

            public string RuleProviderType { get; set; } // = "Unknown";
        }

        public class Template
        {
            public const string DataComponentId = "http://microsoft.com/appmagic/DataComponent";
            public const string UxComponentId = "http://microsoft.com/appmagic/Component";
            public string Id { get; set; }

            // Very important for data components.
            public string Name { get; set; }

            public string Version { get; set; }
            public string LastModifiedTimestamp {get;set;}

            // Used with templates. 
            public bool? IsComponentDefinition { get; set; }
            public ComponentDefinitionInfoJson ComponentDefinitionInfo { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }

        }

        public class Item
        {
            public string Name { get; set; } // Control name 
            public string ControlUniqueId { get; set; }

            public string VariantName { get; set; } = string.Empty;

            public Template Template { get; set; }
            public RuleEntry[] Rules { get; set; }

            public Item[] Children { get; set; }

            // Added later. Don't emit false. 
            // $$$ Or, remove from ExtensionData?
            // public bool HasDynamicProperties { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }


            public Dictionary<string, RuleEntry> GetRules()
            {
                var rules = new Dictionary<string, ControlInfoJson.RuleEntry>();
                foreach (var rule in this.Rules)
                {
                    rules[rule.Property] = rule;
                }
                return rules;
            }
        }

        public Item TopParent { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}