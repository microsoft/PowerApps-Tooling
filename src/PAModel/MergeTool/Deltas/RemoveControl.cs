// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IO;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class RemoveControl(ControlPath parentControlPath, string controlName, bool isInComponent)
    : IDelta
{
    public void Apply(CanvasDocument document)
    {
        var controlSet = isInComponent ? document._components : document._screens;

        // Screen removal
        if (parentControlPath == ControlPath.Empty)
        {
            controlSet.Remove(controlName);
            return;
        }

        // error case?
        if (!controlSet.TryGetValue(parentControlPath.Current, out var control))
            return;

        var path = parentControlPath.Next();
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
            // Already removed
            if (!found) return;
        }

        // Remove the control
        // maybe add error checks here too?
        control.Children = control.Children.Where(child => child.Name.Identifier != controlName).ToList();
        document._editorStateStore.Remove(controlName);
    }
}
