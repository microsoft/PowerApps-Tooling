using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    public class AuthConfigProperty
    {
        public int SectionIndex { get; set; }
        public string HelperUI { get; set; }
        public string PropertyGroupName { get; set; }
        public string SectionName { get; set; }
        public int Type { get; set; }
        public int PropertyKind { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
