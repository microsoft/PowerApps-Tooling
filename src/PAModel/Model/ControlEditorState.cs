// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.PowerPlatform.Formulas.Tools.JsonConverters;

namespace Microsoft.PowerPlatform.Formulas.Tools.Model;

public record ControlEditorState
{
    public string Name { get; init; }

    public string Type { get; set; }

    [JsonConverter(typeof(JsonDoubleToIntConverter))]
    public int Index { get; set; }

    public ControlEditorState[] Children { get; set; }

    public IList<RuleEditorState> Rules { get; init; }
}
