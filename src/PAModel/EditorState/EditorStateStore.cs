// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    internal class EditorStateStore
    {
        // Key is control name, case-sensitive
        private readonly Dictionary<string, ControlState> _controls;

        public EditorStateStore()
        {
            _controls = new Dictionary<string, ControlState>(StringComparer.Ordinal);
        }

        public EditorStateStore(EditorStateStore other)
        {
            _controls = other._controls.JsonClone();
        }

        public bool ContainsControl(string name)
        {
            return _controls.ContainsKey(name);
        }

        public bool TryAddControl(ControlState control)
        {
            if (_controls.ContainsKey(control.Name))
                return false;

            _controls.Add(control.Name, control);
            return true;
        }

        public bool TryGetControlState(string controlName, out ControlState state)
        {
            return _controls.TryGetValue(controlName, out state);
        }

        public void Remove(string controlName)
        {
            _controls.Remove(controlName);
        }

        public IEnumerable<ControlState> GetControlsWithTopParent(string topParent)
        {
            return _controls.Values.Where(ctrl => ctrl.TopParentName == topParent);
        }

        public IEnumerable<ControlState> Contents { get { return _controls.Values; } }
    }
}
