using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using System;
using System.Collections.Generic;
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

        private static IEnumerable<IDelta> UnionDelta(IEnumerable<IDelta> ours, IEnumerable<IDelta> theirs)
        {
            return ours;
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
