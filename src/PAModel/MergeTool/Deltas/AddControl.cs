// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IO;
using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class AddControl(
    ControlPath parentControlPath,
    BlockNode control,
    Dictionary<string, ControlState> controlStates,
    bool isInComponent)
    : IDelta
{
    public string ControlName => control.Name.Identifier;

    public void Apply(CanvasDocument document)
    {
        var controlSet = isInComponent ? document._components : document._screens;

        // Top level addition
        if (parentControlPath == ControlPath.Empty)
        {
            var repairedTopParent = MakeControlTreeCollisionFree(control, controlStates, document._editorStateStore);
            if (repairedTopParent == null)
                return;

            AddControlStates(repairedTopParent, document._editorStateStore);

            controlSet.Add(control.Name.Identifier, repairedTopParent);

            // Add screen to order set to avoid confusing diffs
            if (!isInComponent && !document._screenOrder.Contains(ControlName))
                document._screenOrder.Add(ControlName);

            return;
        }

        // Top Parent was removed
        if (!controlSet.TryGetValue(parentControlPath.Current, out var value))
            return;

        var path = parentControlPath.Next();
        while (path.Current != null)
        {
            var found = false;
            foreach (var child in value.Children)
            {
                if (child.Name.Identifier == path.Current)
                {
                    value = child;
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

        var repairedControl = MakeControlTreeCollisionFree(control, controlStates, document._editorStateStore);
        if (repairedControl == null)
            return;
        AddControlStates(repairedControl, document._editorStateStore);

        value.Children.Add(repairedControl);
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
        if (controlStates.TryGetValue(name, out var state))
        {
            stateStore.TryAddControl(state);
        }
    }
}
