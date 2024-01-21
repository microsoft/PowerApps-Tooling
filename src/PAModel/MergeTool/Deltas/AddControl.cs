// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class AddControl : IDelta
{
    private readonly bool _isInComponent;
    private readonly ControlPath _parentControlPath;
    private readonly BlockNode _control;
    private readonly Dictionary<string, ControlState> _controlStates;

    public string ControlName => _control.Name.Identifier;

    public AddControl(ControlPath parentControlPath, BlockNode control, Dictionary<string, ControlState> controlStates, bool isInComponent)
    {
        _isInComponent = isInComponent;
        _parentControlPath = parentControlPath;
        _control = control;
        _controlStates = controlStates;
    }

    public void Apply(CanvasDocument document)
    {
        var controlSet = _isInComponent ? document._components : document._screens;

        // Top level addition
        if (_parentControlPath == ControlPath.Empty)
        {
            var repairedTopParent = MakeControlTreeCollisionFree(_control, _controlStates, document._editorStateStore);
            if (repairedTopParent == null)
                return;

            AddControlStates(repairedTopParent, document._editorStateStore);

            controlSet.Add(_control.Name.Identifier, repairedTopParent);

            // Add screen to order set to avoid confusing diffs
            if (!_isInComponent && !document._screenOrder.Contains(ControlName))
                document._screenOrder.Add(ControlName);

            return;
        }

        // Top Parent was removed
        if (!controlSet.TryGetValue(_parentControlPath.Current, out var control))
            return;

        var path = _parentControlPath.Next();
        while (path.Current != null)
        {
            var found = false;
            foreach (var child in control.Children)
            {
                if (child.Name.Identifier == path.Current)
                {
                    control = child;
                    path = path.Next();
                    found = true;
                    break;
                }
            }
            // tree was deleted
            if (!found)
            {
                return;
            }
        }

        var repairedControl = MakeControlTreeCollisionFree(_control, _controlStates, document._editorStateStore);
        if (repairedControl == null)
            return;
        AddControlStates(repairedControl, document._editorStateStore);

        control.Children.Add(repairedControl);
    }

    private static BlockNode MakeControlTreeCollisionFree(BlockNode root, Dictionary<string, ControlState> states, EditorStateStore stateStore)
    {
        var name = root.Name.Identifier;
        if (stateStore.ContainsControl(name))
        {
            RemoveStates(root, states);
            return null;
        }

        var children = new List<BlockNode>();
        foreach (var child in root.Children)
        {
            var collisionFreeChild = MakeControlTreeCollisionFree(child, states, stateStore);
            if (collisionFreeChild != null)
                children.Add(collisionFreeChild);
        }
        root.Children = children;

        return root;
    }

    private static void RemoveStates(BlockNode root, Dictionary<string, ControlState> states)
    {
        var name = root.Name.Identifier;
        states.Remove(name);

        foreach (var child in root.Children)
        {
            RemoveStates(child, states);
        }
    }

    private void AddControlStates(BlockNode root, EditorStateStore stateStore)
    {
        foreach (var child in root.Children)
        {
            AddControlStates(child, stateStore);
        }

        var name = root.Name.Identifier;

        // If the state exists, add to merged document
        if (_controlStates.TryGetValue(name, out var state))
        {
            stateStore.TryAddControl(state);
        }
    }
}
