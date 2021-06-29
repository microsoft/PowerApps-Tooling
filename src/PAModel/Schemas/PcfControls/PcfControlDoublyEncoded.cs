using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl
{
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
