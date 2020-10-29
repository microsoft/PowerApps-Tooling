using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    internal class EditorStateStore
    {
        private Dictionary<string, ControlState> _controls;

        public EditorStateStore()
        {
            _controls = new Dictionary<string, ControlState>();
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
    }
}
