// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools;

// Various data that we can save for round-tripping.
// Everything here is optional!!
// Only be written during MsApp. Opaque for source file.
internal class Entropy
{
    // These come from volatile properties in properties.json in the msapp
    internal class PropertyEntropy
    {
        public Dictionary<string, int> ControlCount { get; set; }
        public double? DeserializationLoadTime { get; set; }
        public double? AnalysisLoadTime { get; set; }
        public double? ErrorCount { get; set; }
    }

    // Json serialize these.
    public Dictionary<string, string> TemplateVersions { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public DateTime? HeaderLastSavedDateTimeUTC { get; set; }
    public string OldLogoFileName { get; set; }

    // This tracks the state of whether msapp has AllowGlobalScope property in component instance -> component definition in controls array under screen.
    public bool IsLegacyComponentAllowGlobalScopeCase { get; set; }

    // To fully round-trip, we need to preserve array order for the various un-ordered arrays that we may split apart.
    public Dictionary<string, int> OrderDataSource { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    public Dictionary<string, int> OrderComponentMetadata { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    public Dictionary<string, int> OrderTemplate { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    public Dictionary<string, int> OrderXMLTemplate { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    public Dictionary<string, int> OrderComponentTemplate { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    public Dictionary<string, int> OrderPcfTemplate { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

    // outer key is group control name, inner key is child grouped control name
    public Dictionary<string, Dictionary<string, int>> OrderGroupControls { get; set; } = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

    // Key is component name, value is Index.
    public Dictionary<string, double> ComponentIndexes { get; set; } = new Dictionary<string, double>(StringComparer.Ordinal);

    // Key is new FileName of the duplicate resource, value is Index from Resources.json.
    public Dictionary<string, int> ResourcesJsonIndices { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // Key is control name, value is publish order index
    public Dictionary<string, double> PublishOrderIndices { get; set; } = new Dictionary<string, double>(StringComparer.Ordinal);

    // Key is control name, value is uniqueId (Ids in an app will either be int or Guids but not a mix of both)
    public Dictionary<string, int> ControlUniqueIds { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    public Dictionary<string, Guid> ControlUniqueGuids { get; set; } = new Dictionary<string, Guid>(StringComparer.Ordinal);

    // Key is resource name
    public Dictionary<string, string> LocalResourceRootPaths { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    // Key is resource name, value is filename
    public Dictionary<string, string> LocalResourceFileNames { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // Some Component Function Parameter Properties are serialized with a different InvariantScript and DefaultScript.
    // We show the InvariantScript in the .yaml, and persist the difference in Entropy to ensure we have accurate roundtripping behavior.
    // The reason for using string[] is to have a way to lookup InvariantScript when the template custom properties are updated with default values in RepopulateTemplateCustomProperties.
    // Key is the fully qualified function argument name ('FunctionName'_'ScoreVariableName'), eg. SelectAppointment_AppointmentId
    public Dictionary<string, string[]> FunctionParamsInvariantScripts { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    // Some Component Function Parameter Properties on instances are serialized with a different InvariantScript and DefaultScript.
    public Dictionary<string, string[]> FunctionParamsInvariantScriptsOnInstances { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    // Key is control name, this should be unused if no datatables are present
    public Dictionary<string, string> DataTableCustomControlTemplateJsons { get; set; }

    public PropertyEntropy VolatileProperties { get; set; }

    /// <summary>
    /// Sometimes, an empty LocalDatabaseReferences is serialized as "" or {}.<br />
    /// Need to track which so we can round-trip.<br />
    /// if true: "".   Else,  "{}".<br />
    /// Note: Both possible values of this field indicate that  LocalDatabaseReferences  was empty.
    /// Therefore the value of this field should not be used to check whether LocalDatabaseReferences was empty or not.
    /// </summary>
    public bool LocalDatabaseReferencesAsEmpty { get; set; }

    /// <summary>
    /// Tracks whether LocalDatabaseReferences  was empty or not.
    /// </summary>
    public bool? WasLocalDatabaseReferencesEmpty { get; set; }

    /// <summary>
    /// Tracks whether TestStepsMetadata is empty or not.
    /// </summary>
    public bool? DoesTestStepsMetadataExist { get; set; }

    // Key is connection id, value is connection instance id
    public Dictionary<string, string> LocalConnectionIDReferences { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    // Key is test rule, value is test screen id without Screen name
    public Dictionary<string, string> RuleScreenIdWithoutScreen { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    // Key is control name, value is OverridableProperties
    public Dictionary<string, object> OverridablePropertiesEntry { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    // Key is control name, value is PCFDynamicSchemaForIRRetrieval
    public Dictionary<string, object> PCFDynamicSchemaForIRRetrievalEntry { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    // Key is control name, value is PCFTemplateDetails
    public Dictionary<string, ControlInfoJson.Template> PCFTemplateEntry { get; set; } = new Dictionary<string, ControlInfoJson.Template>(StringComparer.Ordinal);

    public int GetOrder(DataSourceEntry dataSource)
    {
        // To ensure that that TableDefinitions are put at the end in DataSources.json when the order information is not available.
        var defaultValue = dataSource.TableDefinition != null ? int.MaxValue : -1;
        return OrderDataSource.GetOrDefault(dataSource.GetUniqueName(), defaultValue);
    }
    public void Add(DataSourceEntry entry, int? order)
    {
        if (order.HasValue)
        {
            OrderDataSource[entry.GetUniqueName()] = order.Value;
        }
    }

    public int GetOrder(ComponentsMetadataJson.Entry entry)
    {
        return OrderComponentMetadata.GetOrDefault(entry.TemplateName, -1);
    }
    public void Add(ComponentsMetadataJson.Entry entry, int order)
    {
        OrderComponentMetadata[entry.TemplateName] = order;
    }

    public int GetOrder(TemplateMetadataJson entry)
    {
        return OrderTemplate.GetOrDefault(entry.Name, -1);
    }
    public void Add(TemplateMetadataJson entry, int order)
    {
        OrderTemplate[entry.Name] = order;
    }

    public int GetOrder(TemplatesJson.TemplateJson entry)
    {
        return OrderXMLTemplate.GetOrDefault(entry.Name, -1);
    }
    public void Add(TemplatesJson.TemplateJson entry, int order)
    {
        OrderXMLTemplate[entry.Name] = order;
    }

    public int GetComponentOrder(TemplateMetadataJson entry)
    {
        return OrderComponentTemplate.GetOrDefault(entry.Name, -1);
    }
    public void AddComponent(TemplateMetadataJson entry, int order)
    {
        OrderComponentTemplate[entry.Name] = order;
    }

    public int GetPcfVersioning(PcfTemplateJson entry)
    {
        return OrderPcfTemplate.GetOrDefault(entry.Name, -1);
    }
    public void AddPcfVersioning(PcfTemplateJson entry, int order)
    {
        OrderPcfTemplate[entry.Name] = order;
    }

    public void Add(ResourceJson resource, int order)
    {
        if (resource == null)
        {
            return;
        }

        var key = GetResourcesJsonIndicesKey(resource);
        if (!ResourcesJsonIndices.ContainsKey(key))
        {
            ResourcesJsonIndices.Add(key, order);
        }
    }

    // Using the name of the resource combined with the content kind as a key to avoid collisions across different resource types.
    public static string GetResourcesJsonIndicesKey(ResourceJson resource)
    {
        return resource.Content + "-" + resource.Name;
    }

    // The key is of the format ContentKind-ResourceName. eg. Image-close.
    // Removing the 'ContentKind-' gives the resource name
    public static string GetResourceNameFromKey(string key)
    {
        var prefix = key.Split(new char[] { '-' }).First();
        return key.Substring(prefix.Length + 1);
    }

    public void SetHeaderLastSaved(DateTime? x)
    {
        HeaderLastSavedDateTimeUTC = x;
    }
    public DateTime? GetHeaderLastSaved()
    {
        return HeaderLastSavedDateTimeUTC;
    }

    public void SetTemplateVersion(string dataComponentGuid, string version)
    {
        TemplateVersions[dataComponentGuid] = version;
    }

    public string GetTemplateVersion(string dataComponentGuid)
    {
        TemplateVersions.TryGetValue(dataComponentGuid, out var version);

        // Version string is ok to be null.
        // DateTime.Now.ToUniversalTime().Ticks.ToString();
        return version;
    }

    public void SetLogoFileName(string oldLogoName)
    {
        OldLogoFileName = oldLogoName;
    }

    public void SetProperties(DocumentPropertiesJson documentProperties)
    {
        VolatileProperties = new PropertyEntropy()
        {
            AnalysisLoadTime = documentProperties.AnalysisLoadTime,
            DeserializationLoadTime = documentProperties.DeserializationLoadTime,
            ControlCount = documentProperties.ControlCount,
            ErrorCount = documentProperties.ErrorCount
        };

        documentProperties.AnalysisLoadTime = null;
        documentProperties.DeserializationLoadTime = null;
        documentProperties.ControlCount = null;
        documentProperties.ErrorCount = null;
    }

    public void GetProperties(DocumentPropertiesJson documentProperties)
    {
        if (VolatileProperties != null)
        {
            documentProperties.AnalysisLoadTime = VolatileProperties.AnalysisLoadTime;
            documentProperties.DeserializationLoadTime = VolatileProperties.DeserializationLoadTime;
            documentProperties.ControlCount = VolatileProperties.ControlCount;
            documentProperties.ErrorCount = VolatileProperties.ErrorCount;
        }
    }

    public void AddGroupControl(ControlState groupControl)
    {
        var name = groupControl.Name;
        var groupOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        OrderGroupControls[name] = groupOrder;
        var order = 0;
        foreach (var child in groupControl.GroupedControlsKey)
        {
            groupOrder.Add(child, order);
            ++order;
        }
    }

    public int GetGroupControlOrder(string groupName, string childName)
    {
        if (!OrderGroupControls.TryGetValue(groupName, out var groupOrder))
            return -1;

        return groupOrder.GetOrDefault(childName, -1);
    }

    public void AddDataTableControlJson(string controlName, string json)
    {
        DataTableCustomControlTemplateJsons ??= new Dictionary<string, string>();

        DataTableCustomControlTemplateJsons.Add(controlName, json);
    }

    public bool TryGetDataTableControlJson(string controlName, out string json)
    {
        json = null;
        if (DataTableCustomControlTemplateJsons == null)
        {
            return false;
        }

        return DataTableCustomControlTemplateJsons.TryGetValue(controlName, out json);
    }

    public string GetDefaultScript(string propName, string defaultValue)
    {
        if (FunctionParamsInvariantScripts.TryGetValue(propName, out var value) && value?.Length == 2)
        {
            return value[0];
        }
        return defaultValue;
    }

    public string GetInvariantScript(string propName, string defaultValue)
    {
        if (FunctionParamsInvariantScripts.TryGetValue(propName, out var value) && value?.Length == 2)
        {
            return value[1];
        }
        return defaultValue;
    }

    public string GetInvariantScriptOnInstances(string propName, string defaultValue)
    {
        if (FunctionParamsInvariantScriptsOnInstances.TryGetValue(propName, out var value) && value?.Length == 2)
        {
            return value[1];
        }
        return defaultValue;
    }

    public bool IsLocalDatabaseReferencesEmpty()
    {
        return WasLocalDatabaseReferencesEmpty ?? LocalDatabaseReferencesAsEmpty;
    }
}
