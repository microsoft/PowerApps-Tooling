// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState;

/// <summary>
/// Contains property state for AutoLayout properties not written to .pa
/// <see cref="ControlInfoJson.DynamicPropertyJson"/>
/// </summary>
public class DynamicPropertyState
{
    public string PropertyName { get; set; }

    public PropertyState Property { get; set; }

    // Object with additional properties like AFDDataSourceName, etc.
    public object ControlPropertyState { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
}
