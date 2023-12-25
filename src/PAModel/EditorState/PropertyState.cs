// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState;

/// <summary>
/// Contains property state not written to .pa
/// <see cref="ControlInfoJson.RuleEntry"/>
/// After ControlPropertyState is cleaned up by studio, that should get merged into this
/// </summary>
internal class PropertyState
{
    public string PropertyName { get; set; }
    public string NameMap { get; set; }
    public string RuleProviderType { get; set; } // = "Unknown";
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}
