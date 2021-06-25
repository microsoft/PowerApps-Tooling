using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    class PcfControl
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
        public string FullyQualifiedControlName { get; set; }
        public Resource[] Resources { get; set; }
        public IDictionary<string, string> SubscribedFunctionalities { get; set; }
        public IEnumerable<Property> Properties { get; set; }
        public IEnumerable<Property> IncludedProperties { get; set; }
        public IEnumerable<IDictionary<string, AuthConfigProperty>> AuthConfigProperties { get; set; }
        public IEnumerable<PropertyDependency> PropertyDependencies { get; set; }
        public IEnumerable<DataConnectorMetadata> DataConnectors { get; set; }
        public IEnumerable<Event> Events { get; set; }
        public IEnumerable<Event> CommonEvents { get; set; }

        public static PcfControl GetPowerAppsControlFromJson(CombinedTemplateState template)
        {
            var powerAppControl = new PcfControl();

            var dynamicControlDefinition = Utilities.JsonParse<IDictionary<string, string>>(template.DynamicControlDefinitionJson);

            if (dynamicControlDefinition.ContainsKey(NamespaceKey))
            {
                powerAppControl.ControlNamespace = dynamicControlDefinition[NamespaceKey];
            }
            if (dynamicControlDefinition.ContainsKey(ConstructorKey))
            {
                powerAppControl.ControlConstructor = dynamicControlDefinition[ConstructorKey];
            }
            if (dynamicControlDefinition.ContainsKey(ResourcesKey))
            {
                powerAppControl.Resources = Utilities.JsonParse<Resource[]>(dynamicControlDefinition[ResourcesKey]);
            }
            if (dynamicControlDefinition.ContainsKey(PropertiesKey))
            {
                powerAppControl.Properties = Utilities.JsonParse<IEnumerable<Property>>(dynamicControlDefinition[PropertiesKey]);
            }
            if (dynamicControlDefinition.ContainsKey(AuthConfigPropertiesKey))
            {
                var authConfigPropertiesGroup = new List<IDictionary<string, AuthConfigProperty>>();
                using (var doc = JsonDocument.Parse(dynamicControlDefinition[AuthConfigPropertiesKey]))
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
                powerAppControl.AuthConfigProperties = authConfigPropertiesGroup;
            }
            if (dynamicControlDefinition.ContainsKey(DataConnectorsKey))
            {
                powerAppControl.DataConnectors = Utilities.JsonParse<IEnumerable<DataConnectorMetadata>>(dynamicControlDefinition[DataConnectorsKey]);
            }
            if (dynamicControlDefinition.ContainsKey(SubscribedFunctionalitiesKey))
            {
                powerAppControl.SubscribedFunctionalities = Utilities.JsonParse<Dictionary<string, string>>(dynamicControlDefinition[SubscribedFunctionalitiesKey]);
            }
            if (dynamicControlDefinition.ContainsKey(IncludedPropertiesKey))
            {
                powerAppControl.IncludedProperties = Utilities.JsonParse<IEnumerable<Property>>(dynamicControlDefinition[IncludedPropertiesKey]);
            }
            if (dynamicControlDefinition.ContainsKey(EventsKey))
            {
                powerAppControl.Events = Utilities.JsonParse<IEnumerable<Event>>(dynamicControlDefinition[EventsKey]);
            }
            if (dynamicControlDefinition.ContainsKey(CommonEventsKey))
            {
                powerAppControl.CommonEvents = Utilities.JsonParse<IEnumerable<Event>>(dynamicControlDefinition[CommonEventsKey]);
            }
            if (dynamicControlDefinition.ContainsKey(PropertyDependenciesKey))
            {
                powerAppControl.PropertyDependencies = Utilities.JsonParse<IEnumerable<PropertyDependency>>(dynamicControlDefinition[PropertyDependenciesKey]);
            }

            return powerAppControl;
        }

        internal static string GenerateDynamicControlDefinition(PcfControl control)
        {
            // PowerApps controls require dynamic control definition added to control's template.
            IDictionary<string, string> _dynamicControlDefinition = new Dictionary<string, string>
            {
                { NamespaceKey, control.ControlNamespace },
                { ConstructorKey, control.ControlConstructor }
            };

            var jsonOptions = new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            var resources = JsonSerializer.Serialize(control.Resources, jsonOptions);
            _dynamicControlDefinition.Add(ResourcesKey, resources);

            var properties = JsonSerializer.Serialize(control.Properties, jsonOptions);
            _dynamicControlDefinition.Add(PropertiesKey, properties);

            var includedProperties = JsonSerializer.Serialize(control.IncludedProperties, jsonOptions);
            _dynamicControlDefinition.Add(IncludedPropertiesKey, includedProperties);

            if (control.Events?.Count() > 0)
            {
                var events = JsonSerializer.Serialize(control.Events, jsonOptions);
                _dynamicControlDefinition.Add(EventsKey, events);
            }

            if (control.CommonEvents?.Count() > 0)
            {
                var commonEvents = JsonSerializer.Serialize(control.CommonEvents, jsonOptions);
                _dynamicControlDefinition.Add(CommonEventsKey, commonEvents);
            }

            if (control.PropertyDependencies?.Count() > 0)
            {
                var propertyDependencies = JsonSerializer.Serialize(control.PropertyDependencies, jsonOptions);
                _dynamicControlDefinition.Add(PropertyDependenciesKey, propertyDependencies);
            }

            if (control.SubscribedFunctionalities?.Count() > 0)
            {
                var subscribedFunctionalities = JsonSerializer.Serialize(control.SubscribedFunctionalities, jsonOptions);
                _dynamicControlDefinition.Add(SubscribedFunctionalitiesKey, subscribedFunctionalities);
            }

            if (control.AuthConfigProperties?.Count() > 0)
            {
                var authConfigProperties = JsonSerializer.Serialize(control.AuthConfigProperties, jsonOptions);
                _dynamicControlDefinition.Add(AuthConfigPropertiesKey, authConfigProperties);
            }

            if (control.DataConnectors?.Count() > 0)
            {
                var dataConnectors = JsonSerializer.Serialize(control.DataConnectors, jsonOptions);
                _dynamicControlDefinition.Add(DataConnectorsKey, dataConnectors);
            }

            return JsonSerializer.Serialize(_dynamicControlDefinition, jsonOptions);
        }
    }
}
