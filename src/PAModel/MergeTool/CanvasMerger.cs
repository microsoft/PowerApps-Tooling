// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.MergeTool;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
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
            var resultDeltas = new List<IDelta>();

            // If we apply the removes before the adds, adds will fail if they're in a tree that's been removed
            // This is intended but we should talk about it
            resultDeltas.AddRange(ours.OfType<RemoveControl>());
            resultDeltas.AddRange(theirs.OfType<RemoveControl>());

            var ourControlAdds = ours.OfType<AddControl>();
            resultDeltas.AddRange(ourControlAdds);
            resultDeltas.AddRange(theirs.OfType<AddControl>().Where(add => !ourControlAdds.Any(ourAdd => ourAdd.ControlName == add.ControlName)));

            var ourPropChanges = ours.OfType<ChangeProperty>();
            var theirPropChanges = theirs.OfType<ChangeProperty>();
            resultDeltas.AddRange(ourPropChanges);
            resultDeltas.AddRange(theirPropChanges.Where(change => !ourPropChanges.Any(ourChange => ourChange.PropertyName == change.PropertyName && ourChange.ControlPath.Equals(change.ControlPath))));

            var ourTemplateChanges = ours.OfType<AddTemplate>();
            var theirTemplateChanges = theirs.OfType<AddTemplate>();
            // This may need to be smarter for component templates
            resultDeltas.AddRange(ourTemplateChanges);
            resultDeltas.AddRange(theirTemplateChanges);

            resultDeltas.AddRange(ours.OfType<RemoveResource>());
            resultDeltas.AddRange(theirs.OfType<RemoveResource>());

            resultDeltas.AddRange(ours.OfType<AddResource>());
            resultDeltas.AddRange(theirs.OfType<AddResource>());

            resultDeltas.AddRange(theirs.OfType<UpdateResource>());
            resultDeltas.AddRange(ours.OfType<UpdateResource>());

            resultDeltas.AddRange(ours.OfType<RemoveDataSource>());
            resultDeltas.AddRange(theirs.OfType<RemoveDataSource>());

            resultDeltas.AddRange(ours.OfType<AddDataSource>());
            resultDeltas.AddRange(theirs.OfType<AddDataSource>());

            resultDeltas.AddRange(ours.OfType<RemoveConnection>());
            resultDeltas.AddRange(theirs.OfType<RemoveConnection>());

            resultDeltas.AddRange(ours.OfType<AddConnection>());
            resultDeltas.AddRange(theirs.OfType<AddConnection>());

            // Take the local theme only
            resultDeltas.AddRange(ours.OfType<ThemeChange>());

            var ourSettingsChanges = ours.OfType<DocumentPropertiesChange>();
            var theirSettingsChanges = theirs.OfType<DocumentPropertiesChange>();
            resultDeltas.AddRange(ourSettingsChanges);
            resultDeltas.AddRange(theirSettingsChanges.Where(change => !ourSettingsChanges.Any(ourChange => ourChange.Name == change.Name)));

            return resultDeltas;
        }


        private static CanvasDocument ApplyDelta(CanvasDocument parent, IEnumerable<IDelta> delta)
        {
            var result = new CanvasDocument(parent);
            foreach (var change in delta)
            {
                change.Apply(result);
            }

            if (delta.Any())
            {
                result._entropy = new Entropy();
            }
            return result;
        }
    }
}
