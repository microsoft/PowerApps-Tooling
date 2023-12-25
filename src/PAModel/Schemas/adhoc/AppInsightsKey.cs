// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools;

// Abstract out the InstrumentationKey (From AppInsights) of an app in a seprate json file (AppInsightsKey.json)
internal class AppInsightsKeyJson
{
    public string InstrumentationKey { get; set; } // a guid
}
