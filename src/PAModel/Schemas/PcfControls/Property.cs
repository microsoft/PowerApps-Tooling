// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl
{
    internal struct Property
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int Type { get; set; }
        public string DefaultValue { get; set; }
        public string PhoneDefaultValue { get; set; }
        public string WebDefaultValue { get; set; }
        public string NullDefaultValue { get; set; }
        public EnumValue[] EnumValues { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
