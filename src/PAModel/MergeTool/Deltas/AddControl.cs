using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class AddControl : IDelta
    {
        public ControlPath ParentControlPath;
        public BlockNode Control;
        public Dictionary<string, ControlState> ControlStates;

        public void Apply(CanvasDocument document)
        {
            // Screen addition
            if (ParentControlPath == ControlPath.Empty)
            {
                if (!IsControlTreeCollisionFree(this.Control, document._editorStateStore))
                    return;

                AddControlStates(this.Control, document._editorStateStore);

                document._screens.Add(this.Control.Name.Identifier, this.Control);
                return;
            }

            // screen was removed?
            if (!document._screens.TryGetValue(ParentControlPath.Current, out var control))
                return;

            var path = ParentControlPath.Next();
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

            if (!IsControlTreeCollisionFree(this.Control, document._editorStateStore))
                return;

            AddControlStates(this.Control, document._editorStateStore);

            control.Children.Add(this.Control);
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
            if (ControlStates.TryGetValue(name, out var state))
            {
                stateStore.TryAddControl(state);
            }
        }
    }
}
