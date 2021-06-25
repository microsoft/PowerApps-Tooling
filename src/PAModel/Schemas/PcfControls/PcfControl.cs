using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl
{
    internal class PcfControl
    {
        private const string EventsKey = "Events";
        private const string CommonEventsKey = "CommonEvents";
        private const string ResourcesKey = "Resources";
        private const string PropertiesKey = "Properties";
        private const string NamespaceKey = "ControlNamespace";
        private const string ConstructorKey = "ControlConstructor";
        private const string IncludedPropertiesKey = "IncludedProperties";
        private const string PropertyDependenciesKey = "PropertyDependencies";
        private const string SubscribedFunctionalitiesKey = "SubscribedFunctionalities";
        private const string AuthConfigPropertiesKey = "AuthConfigProperties";
        private const string DataConnectorsKey = "DataConnectors";

        public string ControlNamespace { get; set; }
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


        private static IEnumerable<IDictionary<string, AuthConfigProperty>> GetAutConfigProperties(string authConfigPropertiesJson)
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
            var pcfControl = new PcfControl();

            var dynamicControlDefinition = Utilities.JsonParse<PcfControlDoublyEncoded>(template.DynamicControlDefinitionJson);
            pcfControl.ControlNamespace = dynamicControlDefinition.ControlNamespace;
            pcfControl.ControlConstructor = dynamicControlDefinition.ControlConstructor;
            pcfControl.Resources = dynamicControlDefinition.Resources != null ? Utilities.JsonParse<Resource[]>(dynamicControlDefinition.Resources) : null;
            pcfControl.Properties = dynamicControlDefinition.Properties != null ? Utilities.JsonParse<IEnumerable<Property>>(dynamicControlDefinition.Properties) : null;
            pcfControl.AuthConfigProperties = dynamicControlDefinition.AuthConfigProperties != null ? GetAutConfigProperties(dynamicControlDefinition.AuthConfigProperties) : null;
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
            PcfControlDoublyEncoded _dynamicControlDefinition = new PcfControlDoublyEncoded() { ExtensionData = new Dictionary<string, object>() };
            _dynamicControlDefinition.ControlNamespace = control.ControlNamespace;
            _dynamicControlDefinition.ControlConstructor = control.ControlConstructor;
            var jsonOptions = new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            _dynamicControlDefinition.Resources = control.Resources != null ? JsonSerializer.Serialize(control.Resources, jsonOptions) : null;
            _dynamicControlDefinition.Properties = control.Properties != null ? JsonSerializer.Serialize(control.Properties, jsonOptions) : null;
            _dynamicControlDefinition.IncludedProperties = control.IncludedProperties != null ? JsonSerializer.Serialize(control.IncludedProperties, jsonOptions) : null ;
            _dynamicControlDefinition.Events = control.Events != null ? JsonSerializer.Serialize(control.Events, jsonOptions) : null;
            _dynamicControlDefinition.CommonEvents = control.CommonEvents != null ? JsonSerializer.Serialize(control.CommonEvents, jsonOptions) : null;
            _dynamicControlDefinition.PropertyDependencies = control.PropertyDependencies != null ? JsonSerializer.Serialize(control.PropertyDependencies, jsonOptions) : null;
            _dynamicControlDefinition.SubscribedFunctionalities = control.SubscribedFunctionalities != null ? JsonSerializer.Serialize(control.SubscribedFunctionalities, jsonOptions) : null;
            _dynamicControlDefinition.AuthConfigProperties = control.AuthConfigProperties != null ? JsonSerializer.Serialize(control.AuthConfigProperties, jsonOptions) : null;
            _dynamicControlDefinition.DataConnectors = control.DataConnectors != null ? JsonSerializer.Serialize(control.DataConnectors, jsonOptions) : null;

            return Utilities.JsonSerialize(_dynamicControlDefinition);
        }
    }

    internal class PcfControlDoublyEncoded
    {
        public string ControlNamespace { get; set; }
        public string ControlConstructor { get; set; }
        public string FullyQualifiedControlName { get; set; }
        public string Resources { get; set; }
        public string SubscribedFunctionalities { get; set; }
        public string Properties { get; set; }
        public string IncludedProperties { get; set; }
        public string AuthConfigProperties { get; set; }
        public string PropertyDependencies { get; set; }
        public string DataConnectors { get; set; }
        public string Events { get; set; }
        public string CommonEvents { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
