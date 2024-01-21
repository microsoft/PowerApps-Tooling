// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Model;

public record ControlEditorState
{
    public string Name { get; init; }

    public ControlEditorState[] Children { get; set; }

    public IList<RuleEditorState> Rules { get; init; }
}
