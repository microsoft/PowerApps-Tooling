// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState;

internal class EditorStateStore
{
    // Key is control name, case-sensitive
    private readonly Dictionary<string, ControlEditorState> _controls;

    public EditorStateStore()
    {
        _controls = new Dictionary<string, ControlEditorState>(StringComparer.Ordinal);
    }

    public EditorStateStore(EditorStateStore other)
    {
        _controls = other._controls.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone());
    }

    public bool ContainsControl(string name)
    {
        return _controls.ContainsKey(name);
    }

    public bool TryAddControl(ControlEditorState control)
    {
        if (_controls.ContainsKey(control.Name))
            return false;

        _controls.Add(control.Name, control);
        return true;
    }

    public bool TryGetControlState(string controlName, out ControlEditorState state)
    {
        return _controls.TryGetValue(controlName, out state);
    }

    public void Remove(string controlName)
    {
        _controls.Remove(controlName);
    }

    public IEnumerable<ControlEditorState> GetControlsWithTopParent(string topParent)
    {
        return _controls.Values.Where(ctrl => ctrl.TopParentName == topParent);
    }

    public IEnumerable<ControlEditorState> Contents => _controls.Values;
}
