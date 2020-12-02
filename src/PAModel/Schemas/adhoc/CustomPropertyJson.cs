using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    internal class CustomPropertyJson
    {
        internal class ScopeVariableInfo
        {
            public string ScopeVariableName { get; set; }
            public string ParentPropertyName { get; set; }
            public string Description { get; set; }
            public int? ParameterIndex { get; set; }
            public string DefaultRule { get; set; }
            public PropertyDataType? ScopePropertyDataType { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }
        
        internal class PropertyScopeRules
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public ScopeVariableInfo ScopeVariableInfo { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }

        internal class PropertyScope
        {
            public PropertyScopeRules[] PropertyScopeRulesKey { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }

        public string Name { get; set; }
        public string PropertyDataTypeKey { get; set; }
        public string Tooltip { get; set; }
        public PropertyScope PropertyScopeKey { get; set; }

        // Helper, not serialized
        [JsonIgnore]
        public bool IsFunctionProperty => PropertyScopeKey != null;

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
