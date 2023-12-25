// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl;

internal class PcfControl
{
    // Name and Version are not part of the DynamicControlDefinitionJson.
    // These are used generate the filename that is sharded into pkgs/PcfTemplates directory.
    public string Name { get; set; }
    public string Version { get; set; }

    public string ControlNamespace { get; set; }
    public string DisplayNameKey { get; set; }
    public string ControlConstructor { get; set; }
    public Resource[] Resources { get; set; }
    public IDictionary<string, string> SubscribedFunctionalities { get; set; }
    public IEnumerable<Property> Properties { get; set; }
    public IEnumerable<Property> IncludedProperties { get; set; }
    public IEnumerable<IDictionary<string, AuthConfigProperty>> AuthConfigProperties { get; set; }
    public IEnumerable<PropertyDependency> PropertyDependencies { get; set; }
    public IEnumerable<DataConnectorMetadata> DataConnectors { get; set; }
    public IEnumerable<Event> Events { get; set; }
    public IEnumerable<Event> CommonEvents { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }


    private static IEnumerable<IDictionary<string, AuthConfigProperty>> GetAuthConfigProperties(string authConfigPropertiesJson)
    {
        var authConfigPropertiesGroup = new List<IDictionary<string, AuthConfigProperty>>();
        using (var doc = JsonDocument.Parse(authConfigPropertiesJson))
        {
            var je = doc.RootElement;
            foreach (var section in je.EnumerateArray())
            {
                var authConfigProperties = new Dictionary<string, AuthConfigProperty>();
                foreach (var authProperty in section.EnumerateObject())
                {
                    authConfigProperties.Add(authProperty.Name, authProperty.Value.ToObject<AuthConfigProperty>());
                }
                authConfigPropertiesGroup.Add(authConfigProperties);
            }
        }
        return authConfigPropertiesGroup;
    }

    public static PcfControl GetPowerAppsControlFromJson(CombinedTemplateState template)
    {
        var pcfControl = new PcfControl() { Name = template.TemplateDisplayName, Version = template.Version };

        var dynamicControlDefinition = Utilities.JsonParse<PcfControlDoublyEncoded>(template.DynamicControlDefinitionJson);
        pcfControl.ControlNamespace = dynamicControlDefinition.ControlNamespace;
        pcfControl.DisplayNameKey = dynamicControlDefinition.DisplayNameKey;
        pcfControl.ControlConstructor = dynamicControlDefinition.ControlConstructor;
        pcfControl.Resources = dynamicControlDefinition.Resources != null ? Utilities.JsonParse<Resource[]>(dynamicControlDefinition.Resources) : null;
        pcfControl.Properties = dynamicControlDefinition.Properties != null ? Utilities.JsonParse<IEnumerable<Property>>(dynamicControlDefinition.Properties) : null;
        pcfControl.AuthConfigProperties = dynamicControlDefinition.AuthConfigProperties != null ? GetAuthConfigProperties(dynamicControlDefinition.AuthConfigProperties) : null;
        pcfControl.DataConnectors = dynamicControlDefinition.DataConnectors != null ? Utilities.JsonParse<IEnumerable<DataConnectorMetadata>>(dynamicControlDefinition.DataConnectors) : null;
        pcfControl.SubscribedFunctionalities = dynamicControlDefinition.SubscribedFunctionalities != null ? Utilities.JsonParse<Dictionary<string, string>>(dynamicControlDefinition.SubscribedFunctionalities) : null;
        pcfControl.IncludedProperties = dynamicControlDefinition.IncludedProperties != null ? Utilities.JsonParse<IEnumerable<Property>>(dynamicControlDefinition.IncludedProperties) : null;
        pcfControl.Events = dynamicControlDefinition.Events != null ? Utilities.JsonParse<IEnumerable<Event>>(dynamicControlDefinition.Events) : null;
        pcfControl.CommonEvents = dynamicControlDefinition.CommonEvents != null ? Utilities.JsonParse<IEnumerable<Event>>(dynamicControlDefinition.CommonEvents) : null;
        pcfControl.PropertyDependencies = dynamicControlDefinition.PropertyDependencies != null ? Utilities.JsonParse<IEnumerable<PropertyDependency>>(dynamicControlDefinition.PropertyDependencies) : null;
        pcfControl.ExtensionData = dynamicControlDefinition.ExtensionData;

        return pcfControl;
    }

    internal static string GenerateDynamicControlDefinition(PcfControl control)
    {
        // PowerApps controls require dynamic control definition added to control's template.
        // When generating DynamicControlDefinitionJson don't add Name and Version as those aren't part of it.
        PcfControlDoublyEncoded _dynamicControlDefinition = new PcfControlDoublyEncoded();
        _dynamicControlDefinition.ControlNamespace = control.ControlNamespace;
        _dynamicControlDefinition.DisplayNameKey = control.DisplayNameKey;
        _dynamicControlDefinition.ControlConstructor = control.ControlConstructor;
        var jsonOptions = new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        _dynamicControlDefinition.Resources = control.Resources != null ? JsonSerializer.Serialize(control.Resources, jsonOptions) : null;
        _dynamicControlDefinition.Properties = control.Properties != null ? JsonSerializer.Serialize(control.Properties, jsonOptions) : null;
        _dynamicControlDefinition.IncludedProperties = control.IncludedProperties != null ? JsonSerializer.Serialize(control.IncludedProperties, jsonOptions) : null;
        _dynamicControlDefinition.Events = control.Events != null ? JsonSerializer.Serialize(control.Events, jsonOptions) : null;
        _dynamicControlDefinition.CommonEvents = control.CommonEvents != null ? JsonSerializer.Serialize(control.CommonEvents, jsonOptions) : null;
        _dynamicControlDefinition.PropertyDependencies = control.PropertyDependencies != null ? JsonSerializer.Serialize(control.PropertyDependencies, jsonOptions) : null;
        _dynamicControlDefinition.SubscribedFunctionalities = control.SubscribedFunctionalities != null ? JsonSerializer.Serialize(control.SubscribedFunctionalities, jsonOptions) : null;
        _dynamicControlDefinition.AuthConfigProperties = control.AuthConfigProperties != null ? JsonSerializer.Serialize(control.AuthConfigProperties, jsonOptions) : null;
        _dynamicControlDefinition.DataConnectors = control.DataConnectors != null ? JsonSerializer.Serialize(control.DataConnectors, jsonOptions) : null;
        _dynamicControlDefinition.ExtensionData = control.ExtensionData ?? new Dictionary<string, object>();

        return Utilities.JsonSerialize(_dynamicControlDefinition);
    }
}
