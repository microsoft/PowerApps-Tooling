using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas
{
    public class Resource
    {
        public int Type { get; set; }
        public string Path { get; set;  }
        public string ModifiedPath { get;  set; }
        public int LoadingOrder { get; set; }
        public bool IsControlSpecific { get; set;  }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
