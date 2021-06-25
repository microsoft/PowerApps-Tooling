using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    public struct Property
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }
        public int Usage { get; set; }
        public bool Hidden { get; set; }
        public string DefaultValue { get; set; }
        public string PhoneDefaultValue { get; set; }
        public string WebDefaultValue { get; set; }
        public string NullDefaultValue { get; set; }
        public string HelperUI { get; set; }
        public string Category { get; set; }
        public bool IsPrimaryBehavioral { get; set; }
        public bool IsPrimaryInput { get; set; }
        public bool IsPrimaryOutput { get; set; }
        public bool IsResettable { get; set; }
        public EnumValue[] EnumValues { get; set; }
        public bool Required { get; set; }
        public bool IsDataSourceProperty { get; set; }
        public string PassThroughProperty { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
