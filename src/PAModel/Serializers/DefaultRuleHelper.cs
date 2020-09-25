// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.Serializers
{
    // Provides collection of all default rules.
    // Used by reader/writer to add/remove default rules - avoid redundancy.
    // The exact defaulting rules don't actually matter, as long as it's the same for
    // read and write so that we roundtrip. 
    internal class DefaultRuleHelper
    {
        private ControlTemplate _template;
        private Theme _theme;
        private string _styleName;

        public DefaultRuleHelper(
            ControlInfoJson.Item control,
            ControlTemplate template,
            Theme theme)
        {
            _template = template;
            _styleName = control.StyleName;
            _theme = theme;
        }

        // Used on writing to source to omit default rules. 
        public bool TryGetDefaultRule(string propertyName, out string defaultScript)
        {
            var template = _template;

            if (template != null && template.InputDefaults.TryGetValue(propertyName, out defaultScript))
            {
                // Found in template.
                return true;
            }
            else
            {
                // $$$ Theme needs to deal with the %Reserved% stuff. 
                return _theme.TryLookup(_styleName, propertyName, out defaultScript);
            }

        }

        // Used on reading from source. Get full list of rules for this control. 
        public Dictionary<string, string> GetDefaultRules()
        {
            // Add themes first.
            var defaults = new Dictionary<string, string>();

            foreach (var kv in _theme.GetStyle(_styleName))
            {
                defaults[kv.Key] = kv.Value;
            }
            if (_template != null)
            {
                foreach (var kv in _template.InputDefaults)
                {
                    defaults[kv.Key] = kv.Value;
                }
            }

            return defaults;
        }

    }
}
