// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas.PcfControl;

internal class AuthConfigProperty
{
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
}
