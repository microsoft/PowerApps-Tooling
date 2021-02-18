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
                    // removed control
                }
            }
            foreach (var newScreen in child._screens.Where(kvp => !parent._screens.ContainsKey(kvp.Key)))
            {
                // added control
            }

            return delta;
        }
    }
}
