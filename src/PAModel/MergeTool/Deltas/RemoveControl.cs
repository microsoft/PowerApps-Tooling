// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

internal class RemoveControl : IDelta
{
    private readonly ControlPath _parentControlPath;
    private readonly string _controlName;
    private readonly bool _isInComponent;

    public RemoveControl(ControlPath parentControlPath, string controlName, bool isInComponent)
    {
        _parentControlPath = parentControlPath;
        _controlName = controlName;
        _isInComponent = isInComponent;
    }

    public void Apply(CanvasDocument document)
    {
        var controlSet = _isInComponent ? document._components : document._screens;

        // Screen removal
        if (_parentControlPath == ControlPath.Empty)
        {
            controlSet.Remove(_controlName);
            return;
        }

        // error case?
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
            // Already removed
            if (!found) return;
        }

        // Remove the control
        // maybe add error checks here too?
        control.Children = control.Children.Where(child => child.Name.Identifier != _controlName).ToList();
        document._editorStateStore.Remove(_controlName);
    }
}
