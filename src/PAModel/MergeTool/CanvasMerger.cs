using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool
{
    public static class CanvasMerger
    {
        public static CanvasDocument Merge(CanvasDocument ours, CanvasDocument theirs, CanvasDocument commonParent)
        {
            var ourDeltas = Diff.ComputeDelta(commonParent, ours);
            var theirDeltas = Diff.ComputeDelta(commonParent, theirs);

            var resultDelta = UnionDelta(ourDeltas, theirDeltas);


            return ApplyDelta(commonParent, resultDelta);
        }

        // fix these to be hashsets eventually?
        private static IEnumerable<IDelta> UnionDelta(IEnumerable<IDelta> ours, IEnumerable<IDelta> theirs)
        {
            var resultDeltas = new List<IDelta>();

            // If we apply the removes before the adds, adds will fail if they're in a tree that's been removed
            // This is intended but we should talk about it
            resultDeltas.AddRange(ours.OfType<RemoveControl>());
            resultDeltas.AddRange(theirs.OfType<RemoveControl>());

            resultDeltas.AddRange(ours.OfType<AddControl>());
            resultDeltas.AddRange(theirs.OfType<AddControl>());

            var ourPropChanges = ours.OfType<ChangeProperty>();
            var theirPropChanges = theirs.OfType<ChangeProperty>();
            resultDeltas.AddRange(ourPropChanges);
            resultDeltas.AddRange(theirPropChanges.Where(change => !ourPropChanges.Any(ourChange => ourChange.PropertyName == change.PropertyName && ourChange.ControlPath.Equals(change.ControlPath))));
            return resultDeltas;
        }


        // this is not correct, we should clone parent and then apply changes
        // it's ok for now though
        private static CanvasDocument ApplyDelta(CanvasDocument parent, IEnumerable<IDelta> delta)
        {
            foreach (var change in delta)
            {
                change.Apply(parent);
            }
            return parent;
        }
    }
}
