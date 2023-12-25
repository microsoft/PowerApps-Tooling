// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl;

internal class PcfControlDoublyEncoded
{
    public string ControlNamespace { get; set; }
    public string DisplayNameKey { get; set; }
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
