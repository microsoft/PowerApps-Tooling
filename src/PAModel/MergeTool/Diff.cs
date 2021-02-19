using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool
{
    internal static class Diff
    {
        public static IEnumerable<IDelta> ComputeDelta(CanvasDocument parent, CanvasDocument child)
        {
            var delta = new List<IDelta>();
            foreach (var originalScreen in parent._screens)
            {
                if (child._screens.TryGetValue(originalScreen.Key, out var childScreen))
                {
                    delta.AddRange(ControlDiffVisitor.GetControlDelta(childScreen, originalScreen.Value));
                }
                else
                {
                    delta.Add(new RemoveControl() { ControlName = originalScreen.Key, ParentControlPath = ControlPath.Empty });
                }
            }
            foreach (var newScreen in child._screens.Where(kvp => !parent._screens.ContainsKey(kvp.Key)))
            {
                delta.Add(new AddControl() { Control = newScreen.Value, ControlStates = child._editorStateStore.GetControlsWithTopParent(newScreen.Key).ToDictionary(state => state.Name), ParentControlPath = ControlPath.Empty });
            }

            return delta;
        }
    }
}
