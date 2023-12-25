// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using System.Text.Json.Serialization;

namespace Microsoft.AppMagic.Authoring.Persistence;

internal enum DataComponentDependencyKind
{
    Cds,
    DataComponent,
    Unknown
}

internal enum DataComponentDefinitionKind
{
    /// <summary>
    /// Data component type mirrors a CDS entity.
    /// </summary>
    Extension,

    /// <summary>
    /// Component has its own type. e.g. combination of one or more entities
    /// </summary>
    Composition,

    /// <summary>
    /// Not yet defined.
    /// </summary>
    Unknown
}

internal class CdsDataDependencyJson
{
    public string LogicalName { get; set; }
    public string DataSetName { get; set; }
}

internal class DataComponentDependencyJson
{
    public string DataComponentTemplateName { get; set; }
}

internal class DataComponentDataDependencyJson
{
    public DataComponentDependencyKind DataComponentExternalDependencyKind { get; set; }
    public CdsDataDependencyJson DataComponentCdsDependency { get; set; }
    public DataComponentDependencyJson DataComponentDependency { get; set; }
}

/// <summary>
/// Schematic class which represents a data component definition, i.e. names and template
/// </summary>
internal class DataComponentDefinitionJson
{
    public string PreferredName { get; set; }
    public string LogicalName { get; set; }
    public string DependentEntityName { get; set; }
    public string ComponentRawMetadataKey { get; set; }
    public string ControlUniqueId { get; set; }
    public DataComponentDefinitionKind DataComponentKind { get; set; }
    public DataComponentDataDependencyJson[] DataComponentExternalDependencies { get; set; }
}


internal class ComponentDefinitionInfoJson
{
    public string Name { get; set; }
    public string LastModifiedTimestamp { get; set; } // "637335420246436668",
    public ControlInfoJson.RuleEntry[] Rules { get; set; }
    public ControlInfoJson.Item[] Children { get; set; }
    public bool? AllowAccessToGlobals { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }

    public ComponentDefinitionInfoJson() { }

    public ComponentDefinitionInfoJson(ControlInfoJson.Item item, string timestamp, ControlInfoJson.Item[] children, bool? allowAccessToGlobals)
    {
        Name = item.Name;
        LastModifiedTimestamp = timestamp;
        Rules = item.Rules;
        Children = children;
        AllowAccessToGlobals = allowAccessToGlobals;

        // Once ControlPropertyState has an actual schema, this can be cleaned up.
        if (item.ExtensionData.ContainsKey("ControlPropertyState"))
        {
            ExtensionData = new Dictionary<string, object>();
            ExtensionData["ControlPropertyState"] = item.ExtensionData["ControlPropertyState"];
        }
    }
}
