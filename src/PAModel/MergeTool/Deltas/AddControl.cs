// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class AddControl : IDelta
    {
        private bool _isInComponent;
        private ControlPath _parentControlPath;
        private BlockNode _control;
        private Dictionary<string, ControlState> _controlStates;

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
                if (!IsControlTreeCollisionFree(_control, document._editorStateStore))
                    return;

                AddControlStates(_control, document._editorStateStore);

                controlSet.Add(_control.Name.Identifier, _control);

                // Add screen to order set to avoid confusing diffs
                if (!_isInComponent)
                    document._screenOrder.Add(_control.Name.Identifier);
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

            if (!IsControlTreeCollisionFree(_control, document._editorStateStore))
                return;

            AddControlStates(_control, document._editorStateStore);

            control.Children.Add(_control);
        }

        private bool IsControlTreeCollisionFree(BlockNode root, EditorStateStore stateStore)
        {
            bool valid = true;
            foreach (var child in root.Children)
            {
                valid &= IsControlTreeCollisionFree(child, stateStore);
            }

            var name = root.Name.Identifier;
            return valid && !stateStore.ContainsControl(name);
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
}
