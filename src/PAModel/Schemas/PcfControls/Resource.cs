// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl
{
    internal class Resource
    {
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
