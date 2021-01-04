// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            string styleName,
            ControlTemplate template,
            Theme theme)
        {
            _template = template;
            _styleName = styleName;
            _theme = theme;
        }

        // Used on writing to source to omit default rules. 
        public bool TryGetDefaultRule(string propertyName, out string defaultScript)
        {
            // Themes (styles) are higher precedence  then Template XML. 
            var template = _template;

            if (_theme.TryLookup(_styleName, propertyName, out defaultScript))
            {
                if (IsLocalizationKey(defaultScript))
                    return false;
                return true;
            }

            if (template != null && template.InputDefaults.TryGetValue(propertyName, out defaultScript))
            {
                if (IsLocalizationKey(defaultScript))
                    return false;

                // Found in template.
                return true;
            }
            defaultScript = null;
            return false;            
        }

        // Used on reading from source. Get full list of rules for this control. 
        public Dictionary<string, string> GetDefaultRules()
        {
            // Add themes first.
            var defaults = new Dictionary<string, string>();

            if (_template != null)
            {
                defaults.AddRange(_template.InputDefaults.Where(kvp => !IsLocalizationKey(kvp.Value)));
            }

            defaults.AddRange(_theme.GetStyle(_styleName).Where(kvp => !IsLocalizationKey(kvp.Value)));

            return defaults;
        }

        // Helper to detect localization key default rules
        // Some default rules like Label.Text are references into localization files
        // Studio replaces them at design-time with the text from the author's current locale.
        // We can't do that here, so we ignore localizationkey default rules when processing defaults
        private readonly Regex _localizationRegex = new Regex("##(\\w+?)##");
        private bool IsLocalizationKey(string rule)
        {
            return _localizationRegex.IsMatch(rule);
        }

    }
}
