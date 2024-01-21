// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState;

/// <summary>
/// Represents the top level of an editor state and its control tree.
/// </summary>
internal class ControlTreeState
{
    /// <summary>
    /// The name of the top level control for the editor state.
    /// Every control in `ControlStates` will have this set as
    /// the `TopParentControl`.
    /// </summary>
    public string TopParentName { get; set; }

    /// <summary>
    /// Collection of <seealso cref="ControlState"/> objects for
    /// this editor state.
    /// </summary>
    public Dictionary<string, ControlState> ControlStates { get; set; }
}
