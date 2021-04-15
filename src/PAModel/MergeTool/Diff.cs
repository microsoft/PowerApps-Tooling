// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
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
                    delta.AddRange(ControlDiffVisitor.GetControlDelta(childScreen, originalScreen.Value, parent._editorStateStore));
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

            var childTemplatesDict = child._templates.UsedTemplates.ToDictionary(temp => temp.Name.ToLower());
            foreach (var template in child._templateStore.Contents)
            {
                if (parent._templateStore.TryGetTemplate(template.Key, out _))
                    continue;

                childTemplatesDict.TryGetValue(template.Key.ToLower(), out var jsonTemplate); 

                delta.Add(new AddTemplate() { Name = template.Key, Template = template.Value, JsonTemplate = jsonTemplate });
            }

            return delta;
        }
    }
}
