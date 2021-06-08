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
        private string _templateName;
        private string _variantName;
        private Theme _theme;
        private string _styleName;
        private bool _inResponsiveContext;

        public DefaultRuleHelper(
            string styleName,
            ControlTemplate template,
            string templateName,
            string variantName,
            Theme theme,
            bool inResponsiveContext)
        {
            _template = template;
            _templateName = templateName;
            _variantName = variantName;
            _styleName = styleName;
            _theme = theme;
            _inResponsiveContext = inResponsiveContext;
        }

        // Used on writing to source to omit default rules. 
        public bool TryGetDefaultRule(string propertyName, out string defaultScript)
        {
            // Themes (styles) are higher precedence  then Template XML. 
            var template = _template;

            if (_theme.TryLookup(_styleName, propertyName, out defaultScript))
            {
                if (ControlTemplateParser.IsLocalizationKey(defaultScript))
                    return false;
                return true;
            }

            // Check template variant first, then template base
            if (template != null &&
                ((_variantName != null &&
                template.VariantDefaultValues.TryGetValue(_variantName, out var defaults) &&
                defaults.TryGetValue(propertyName, out defaultScript)) ||
                template.InputDefaults.TryGetValue(propertyName, out defaultScript)))
            {
                if (ControlTemplateParser.IsLocalizationKey(defaultScript))
                    return false;

                // Found in template.
                return true;
            }

            if (_inResponsiveContext && DynamicProperties.TryGetDefaultValue(propertyName, _templateName, this, out defaultScript))
            {
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
            var variantDefaults = new Dictionary<string, string>();

            if (_template != null)
            {
                // Default values from the variants take precedence over the base template
                var hasVariantDefaults = _variantName != null && _template.VariantDefaultValues.TryGetValue(_variantName, out variantDefaults);
                if (hasVariantDefaults)
                    defaults.AddRange(variantDefaults);

                defaults.AddRange(_template.InputDefaults.Where(kvp => !ControlTemplateParser.IsLocalizationKey(kvp.Value) && !(hasVariantDefaults && variantDefaults.ContainsKey(kvp.Key))));
            }

            defaults.AddRange(_theme.GetStyle(_styleName).Where(kvp => !ControlTemplateParser.IsLocalizationKey(kvp.Value)));

            if (_inResponsiveContext)
            {
                defaults.AddRange(DynamicProperties.GetDefaultValues(_templateName, this));
            }

            return defaults;
        }
    }
}
