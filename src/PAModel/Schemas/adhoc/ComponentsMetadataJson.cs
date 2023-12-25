// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools;

// A minimal representation of the data component manifest
// This is client-only. 
// $$$ - can we get this from the PA file directly?
internal class ComponentManifest
{
    public string Name { get; set; } // a name, "Component1"
    public string TemplateGuid { get; set; } // a guid 
                                             // public string Description { get; set; }

    public bool? AllowAccessToGlobals { get; set; }

    // Other properties in ComponentsMetadataJson.Entry
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }

    public DataComponentDefinitionJson DataComponentDefinitionKey { get; set; }

    // For analysis.
    internal ControlInfoJson _sources;

    internal bool IsDataComponent => this.DataComponentDefinitionKey != null;

    // Only data components have this. 
    internal void Apply(TemplateMetadataJson x)
    {
        x.Validate();

        // $$$ Consistency checks? Or we can just catch this on round-tripping? 
        SetGuid(x.Name);

        this.DataComponentDefinitionKey = x.DataComponentDefinitionKey;
        
        // Clear out volatile state. Will repopulate on write. 
        this.DataComponentDefinitionKey.ControlUniqueId = null; 
    }

    // A component will always have this. 
    internal static ComponentManifest Create(ComponentsMetadataJson.Entry x)
    {
        var dc = new ComponentManifest
        {
            Name = x.Name,
            AllowAccessToGlobals = x.AllowAccessToGlobals,
            ExtensionData = x.ExtensionData
        };
        dc.SetGuid(x.TemplateName);
        return dc;
    }

    private void SetGuid(string guid)
    {
        if (this.TemplateGuid == null)
        {
            this.TemplateGuid = guid;
            return;
        }
        if (this.TemplateGuid != guid)
        {
            throw new InvalidOperationException(); // Mismatch
        }
    }
}


// We recreate this file from the min version. 
// Writes to \references\DataComponentSources.json
internal class DataComponentSourcesJson
{
    public const string NativeCDSDataSourceInfo = "NativeCDSDataSourceInfo";

    // Copy verbatim over. 
    // Should be "portable" - doesn't have fields like Version, timestamp, etc. 
    public class Entry
    {
        // The template guid
        public string AssociatedDataComponentTemplate { get; set; } 

        public string Name { get; set; } // Name of data source, eg, Component1_Table
        public string Type { get; set; } // NativeCDSDataSourceInfo
                 
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    public Entry[] DataSources { get; set; }

    // [JsonExtensionData]
    // public Dictionary<string, JsonElement> ExtensionData { get; set; }
}

// Writes to \References\DataComponentTemplates.json
internal class DataComponentTemplatesJson
{
    public TemplateMetadataJson[] ComponentTemplates { get; set; }

    // Should be empty...
    // [JsonExtensionData]
    // public Dictionary<string, JsonElement> ExtensionData { get; set; }
}



// This is used for both UI components and data components. 
// Writes to \ComponentsMetadata.json
internal class ComponentsMetadataJson
{
    public class Entry
    {
        public string Name { get; set;  } // "Component1";
        public string TemplateName { get; set; } // "a70e51d571ae4649a16b8bf1622ffdac";
        public bool? AllowAccessToGlobals { get; set; }

        // public string Description { get; set; }
        // public bool AllowCustomization { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }

    // Order her is random on server. 
    public Entry[] Components { get; set; }

    // [JsonExtensionData]
    // public Dictionary<string, JsonElement> ExtensionData { get; set; }
}
