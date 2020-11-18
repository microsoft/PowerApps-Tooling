using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    // A combination of the control templates present in Templates.json and the control files
    internal class CombinedTemplateState
    {
        public string Id { get; set; }

        // Very important for data components.
        public string Name { get; set; }

        public string Version { get; set; }
        public string LastModifiedTimestamp { get; set; }

        // Used with templates. 
        public bool? IsComponentTemplate { get; set; }
        public ComponentDefinitionInfoJson ComponentDefinitionInfo { get; set; } = null;

        // Present for component templates with functions
        public CustomPropertyJson[] CustomProperties { get; set; }

        public bool? IsComponentLocked { get; set; } = null;
        public bool? ComponentChangedSinceFileImport { get; set; } = null;
        public bool? ComponentAllowCustomization { get; set; } = null;
        public string TemplateOriginalName { get; set; } = null;


        // Present on PCF
        public string TemplateDisplayName { get; set; } = null;

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }


        public CombinedTemplateState() { }

        public CombinedTemplateState(ControlInfoJson.Template template)
        {
            Id = template.Id;
            Name = template.Name;
            Version = template.Version;
            LastModifiedTimestamp = template.LastModifiedTimestamp;
            ComponentDefinitionInfo = null;
            IsComponentTemplate = template.IsComponentDefinition;
            CustomProperties = template.CustomProperties;
            TemplateDisplayName = template.TemplateDisplayName;
            ExtensionData = template.ExtensionData;
        }

        public ControlInfoJson.Template ToControlInfoTemplate()
        {
            return new ControlInfoJson.Template()
            {
                Id = Id,
                Name = Name,
                Version = Version,
                LastModifiedTimestamp = LastModifiedTimestamp,
                IsComponentDefinition = IsComponentTemplate,
                ComponentDefinitionInfo = ComponentDefinitionInfo,
                CustomProperties = CustomProperties,
                TemplateDisplayName = TemplateDisplayName,
                ExtensionData = ExtensionData,
            };
        }

        public TemplateMetadataJson ToTemplateMetadata()
        {
            return new TemplateMetadataJson()
            {
                Name = Name,
                OriginalName = TemplateOriginalName,
                Version = Version,
                CustomProperties = CustomProperties,
                IsComponentLocked = IsComponentLocked,
                ComponentChangedSinceFileImport = ComponentChangedSinceFileImport,
                ComponentAllowCustomization = ComponentAllowCustomization,
            };
        }
    }
}
