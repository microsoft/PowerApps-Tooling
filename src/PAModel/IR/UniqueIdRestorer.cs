// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.IR
{
    internal class UniqueIdRestorer
    {
        private Dictionary<string, int> _controlUniqueIds;
        private int _nextId;

        public UniqueIdRestorer(Entropy entropy)
        {
            _controlUniqueIds = entropy.ControlUniqueIds;
            _nextId = _controlUniqueIds.Any() ? Math.Max(2, _controlUniqueIds.Values.Max()) : 2;
        }

        public int GetControlId(string controlName)
        {
            if (_controlUniqueIds.TryGetValue(controlName, out var id))
                return id;
            if (controlName == "App")
                return 1;

            return _nextId++;
        }
    }
}
