using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    [DebuggerDisplay("{string.Join('.', _segments)}")]
    internal class ControlPath
    {
        private List<string> _segments;

        public string Current => _segments.Any() ? _segments[0] : null;

        public ControlPath Next()
        {
            var newSegments = new List<string>();
            for (int i = 1; i < _segments.Count; ++i)
            {
                newSegments.Add(_segments[i]);
            }
            return new ControlPath(newSegments);
        }



        public ControlPath Append(string controlName)
        {
            var newpath = new List<string>(_segments);
            newpath.Add(controlName);
            return new ControlPath(newpath);
        }

        public ControlPath(List<string> segments)
        {
            _segments = segments;
        }

        public override bool Equals(object obj)
        {
            return obj is ControlPath other &&
                other._segments.Count == _segments.Count &&
                // I know this is wrong, but it works for now. I'll fix it later.
                string.Join(".", _segments) == string.Join(".", other._segments);
        }

        public override int GetHashCode()
        {
            return _segments.GetHashCode();
        }
    }
}
