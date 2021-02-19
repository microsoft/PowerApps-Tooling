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
                var screen = PrepareControlTree(Control, document._editorStateStore);
                document._screens.Add(screen.Name.Identifier, screen);
                return;
            }

            // error case?
            if (!document._screens.TryGetValue(ParentControlPath.Current, out var control))
                return;

            var path = ParentControlPath.Next();
            while (path.Current != null)
            {
                foreach (var child in control.Children)
                {
                    if (child.Name.Identifier == path.Current)
                    {
                        control = child;
                        path = path.Next();
                        break;
                    }
                }
            }

            control.Children.Add(PrepareControlTree(control, document._editorStateStore));
        }

        private BlockNode PrepareControlTree(BlockNode root, EditorStateStore stateStore)
        {
            var newChildren = new List<BlockNode>();
            foreach (var child in root.Children)
            {
                newChildren.Add(PrepareControlTree(child, stateStore));
            }

            var name = root.Name.Identifier;
            var newName = GetUniqueControlName(name, stateStore);
            if (!ControlStates.TryGetValue(name, out var state))
            {
                throw new NotImplementedException();
            }
            state.Name = newName;
            stateStore.TryAddControl(state);

            return new BlockNode()
            {
                Name = new TypedNameNode() { Identifier = newName, Kind = root.Name.Kind, SourceSpan = root.Name.SourceSpan },
                Children = newChildren,
                Properties = root.Properties,
                Functions = root.Functions,
                SourceSpan = root.SourceSpan,
            };
        }

        private string GetUniqueControlName(string baseName, EditorStateStore stateStore)
        {
            if (!stateStore.ContainsControl(baseName))
                return baseName;

            int i = 1;
            while (stateStore.ContainsControl($"{baseName}_{i}"))
                ++i;

            return $"{baseName}_{i}";
        }
    }
}
