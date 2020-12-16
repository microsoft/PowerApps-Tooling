// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class ControlInfoJson
    {
        public class RuleEntry
        {
            public string Property { get; set; }

            // The PA formulas!
            public string InvariantScript { get; set; }

            public string NameMap { get; set; }

            public string RuleProviderType { get; set; } // = "Unknown";

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }

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

            // Present for component templates with functions
            public CustomPropertyJson[] CustomProperties { get; set; }
            public ComponentType? ComponentType { get; set; } = null;

            // Present on PCF
            public string TemplateDisplayName { get; set; } = null;

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }

            public Template() { }

            public Template(Template other)
            {
                Id = other.Id;
                Name = other.Name;
                TemplateDisplayName = other.TemplateDisplayName;
                Version = other.Version;
                LastModifiedTimestamp = other.LastModifiedTimestamp;
                IsComponentDefinition = other.IsComponentDefinition;
                ComponentDefinitionInfo = other.ComponentDefinitionInfo;
                CustomProperties = other.CustomProperties;
                ExtensionData = other.ExtensionData;
            }

            public static Template CreateDefaultTemplate(string name, ControlTemplate controlTemplate)
            {
                var template = new Template();
                template.Name = name;
                // Try recreating template using template defaults
                if (controlTemplate != null)
                {
                    template.Id = controlTemplate.Id;
                    template.Version = controlTemplate.Version;
                    template.IsComponentDefinition = false;
                    template.LastModifiedTimestamp = "0";
                    template.ExtensionData = new Dictionary<string, object>();
                    template.ExtensionData.Add("FirstParty", true);
                    template.ExtensionData.Add("IsCustomGroupControlTemplate", false);
                    template.ExtensionData.Add("CustomGroupControlTemplateName", "");
                    template.ExtensionData.Add("OverridableProperties", new object());
                }
                return template;
            }
        }

        public class Item
        {
            public string Name { get; set; } // Control name
            public string ControlUniqueId { get; set; }
            public string VariantName { get; set; } = string.Empty;
            public string Parent { get; set; } = string.Empty;
            public double PublishOrderIndex { get; set; }
            public Template Template { get; set; }
            public RuleEntry[] Rules { get; set; }
            public Item[] Children { get; set; }
            public double Index { get; set; } = 0.0;

            // For matching up within a Theme.
            public string StyleName { get; set; }


            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }

            /// These properties should be part of the IR and studio state stuff, but not part of the json
            /// Split these out when refactoring
            [JsonIgnore]    
            public bool SkipWriteToSource { get; set; } = false;

            public Dictionary<string, RuleEntry> GetRules()
            {
                var rules = new Dictionary<string, ControlInfoJson.RuleEntry>();
                foreach (var rule in this.Rules)
                {
                    rules[rule.Property] = rule;
                }
                return rules;
            }

            private static int _id = 2;
            public static Item CreateDefaultControl(ControlTemplate templateDefault = null)
            {
                var defaultCtrl = new Item();
                var rules = new List<RuleEntry>();
                defaultCtrl.Rules = rules.ToArray();
                defaultCtrl.ControlUniqueId = _id.ToString();
                defaultCtrl.PublishOrderIndex = 0;
                ++_id;

                defaultCtrl.ExtensionData = new Dictionary<string, object>();
                defaultCtrl.ExtensionData.Add("LayoutName", "");
                defaultCtrl.ExtensionData.Add("MetaDataIDKey", "");
                defaultCtrl.ExtensionData.Add("PersistMetaDataIDKey", false);
                defaultCtrl.ExtensionData.Add("IsFromScreenLayout", false);
                defaultCtrl.ExtensionData.Add("StyleName", "");
                defaultCtrl.ExtensionData.Add("IsDataControl", false);
                defaultCtrl.ExtensionData.Add("IsGroupControl", false);
                defaultCtrl.ExtensionData.Add("IsAutoGenerated", false);
                defaultCtrl.ExtensionData.Add("IsLocked", false);
                return defaultCtrl;
            }
        }

        public Item TopParent { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}
