// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class RemoveControl : IDelta
    {
        public ControlPath ParentControlPath;
        public string ControlName;


        public void Apply(CanvasDocument document)
        {
            // Screen removal
            if (ParentControlPath == ControlPath.Empty)
            {
                document._screens.Remove(ControlName);
                return;
            }

            // error case?
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
                // Already removed
                if (!found) return;
            }

            // Remove the control
            // maybe add error checks here too?
            control.Children = control.Children.Where(child => child.Name.Identifier != ControlName).ToList();
            document._editorStateStore.Remove(ControlName);
        }
    }
}
