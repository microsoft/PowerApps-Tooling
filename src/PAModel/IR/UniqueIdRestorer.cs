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
        private readonly Dictionary<string, int> _controlUniqueIds;
        private readonly Dictionary<string, Guid> _controlUniqueGuids;
        private int _nextId;

        public UniqueIdRestorer(Entropy entropy)
        {
            _controlUniqueIds = entropy.ControlUniqueIds;
            _controlUniqueGuids = entropy.ControlUniqueGuids;
            _nextId = (_controlUniqueIds.Any() ? Math.Max(2, _controlUniqueIds.Values.Max()) : 2) + 1;
        }

        public string GetControlId(string controlName)
        {
            if (controlName == "App")
            {
                if (_controlUniqueIds.Count > 0)
                {
                    return "1";
                }
                else
                {
                    if (_controlUniqueGuids.TryGetValue(controlName, out var appGuid))
                    {
                        return appGuid.ToString();
                    }
                    else
                    {
                        var newGuid = Guid.NewGuid();
                        _controlUniqueGuids[controlName] = newGuid;
                        return newGuid.ToString();
                    }
                }
            }

            if (_controlUniqueGuids.TryGetValue(controlName, out var guid))
            {
                return guid.ToString();
            }

            if (_controlUniqueIds.TryGetValue(controlName, out var id))
                return id.ToString();

            var nextId = _nextId++;
            _controlUniqueIds.Add(controlName, nextId);
            return nextId.ToString();
        }
    }
}
