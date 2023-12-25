// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;

namespace Microsoft.PowerPlatform.Formulas.Tools;

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
        public const string CommandComponentId = "http://microsoft.com/appmagic/CommandComponent";
        public const string PcfControl = "http://microsoft.com/appmagic/powercontrol";
        public const string HostControl = "http://microsoft.com/appmagic/hostcontrol";

        public string Id { get; set; }

        // Very important for data components.
        public string Name { get; set; }

        public string Version { get; set; }
        public string LastModifiedTimestamp { get; set; }

        // Used with templates.
        public bool? IsComponentDefinition { get; set; }
        public ComponentDefinitionInfoJson ComponentDefinitionInfo { get; set; }

        // Present for component templates with functions
        public CustomPropertyJson[] CustomProperties { get; set; }
        public ComponentType? ComponentType { get; set; } = null;

        // Present on PCF
        public string TemplateDisplayName { get; set; } = null;
        public bool? FirstParty { get; set; }
        public string DynamicControlDefinitionJson { get; set; }

        // Present on Legacy DataTable columns
        public string CustomControlDefinitionJson { get; set; } = null;

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
            CustomControlDefinitionJson = other.CustomControlDefinitionJson;
            CustomProperties = other.CustomProperties;
            ExtensionData = other.ExtensionData;
        }

        public static Template CreateDefaultTemplate(string name, ControlTemplate controlTemplate)
        {
            var template = new Template
            {
                Name = name
            };
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

    public class DynamicPropertyJson
    {
        public string PropertyName { get; set; }
        public RuleEntry Rule { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

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

        public List<string> GroupedControlsKey { get; set; }
        public bool IsGroupControl { get; set; } = false;

        // Present on children of AutoLayout controls
        public DynamicPropertyJson[] DynamicProperties { get; set; } = null;

        // Present on children of AutoLayout controls
        public bool? HasDynamicProperties { get; set; }

        public bool? AllowAccessToGlobals { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

        /// These properties should be part of the IR and studio state stuff, but not part of the json
        /// Split these out when refactoring
        [JsonIgnore]
        public bool SkipWriteToSource { get; set; } = false;

        public Dictionary<string, RuleEntry> GetRules()
        {
            var rules = new Dictionary<string, RuleEntry>();
            foreach (var rule in Rules)
            {
                rules[rule.Property] = rule;
            }
            return rules;
        }

        public static Item CreateDefaultControl(ControlTemplate templateDefault = null)
        {
            var defaultCtrl = new Item();
            var rules = new List<RuleEntry>();
            defaultCtrl.Rules = rules.ToArray();
            defaultCtrl.PublishOrderIndex = 0;

            defaultCtrl.ExtensionData = CreateDefaultExtensionData();
            return defaultCtrl;
        }

        public static Dictionary<string, object> CreateDefaultExtensionData()
        {
            var extensionData = new Dictionary<string, object>();
            extensionData.Add("LayoutName", "");
            extensionData.Add("MetaDataIDKey", "");
            extensionData.Add("PersistMetaDataIDKey", false);
            extensionData.Add("IsFromScreenLayout", false);
            extensionData.Add("IsDataControl", false);
            extensionData.Add("IsAutoGenerated", false);
            extensionData.Add("IsLocked", false);
            return extensionData;
        }
    }

    public Item TopParent { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}
