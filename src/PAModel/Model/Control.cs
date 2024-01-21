// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.PowerPlatform.Formulas.Tools.JsonConverters;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Model;

[DebuggerDisplay("{Name}")]
public record Control
{
    public Control()
    {

    }

    public Control(ControlEditorState editorState)
    {
        EditorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        Name = editorState.Name;

        if (editorState.Children != null)
        {
            var childControls = new List<Control>();
            foreach (var child in editorState.Children)
            {
                childControls.Add(new Control(child));
            }
            Controls = childControls;
            editorState.Children = null;
        }
    }

    [YamlIgnore]
    public ControlEditorState EditorState { get; set; }

    public string Name { get; init; }

    public string Type { get; set; }

    [JsonConverter(typeof(JsonDoubleToIntConverter))]
    public int Index { get; set; }

    public IList<Control> Controls { get; init; }

    public IDictionary<string, object> Properties { get; init; }
}
