// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Abstract out the InstrumentationKey (From AppInsights) of an app in a seprate json file (AppInsightsKey.json)
    class AppInsightsKeyJson
    {
        public string InstrumentationKey { get; set; } // a guid

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}
