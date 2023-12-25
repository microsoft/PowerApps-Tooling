// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    // Responsible for handling dynamic properties and their default vaues
    internal static class DynamicProperties
    {
        // Group container controls add dynamic properties to their children
        private const string GroupContainerTemplate = "groupContainer";

        // ... Except for the manual layout variant. It uses the old x/y layout style
        private const string ManualLayoutVariant = "manualLayoutContainer";

        internal static bool AddsChildDynamicProperties(string template, string variant)
        {
            return template == GroupContainerTemplate && variant != ManualLayoutVariant;
        }


        private static HashSet<string> _supportsNestedControls = new HashSet<string>()
        {
            "dataCard",
            "dataGrid",
            "dataTable",
            "dataTableColumn",
            "datatableHeaderCellCard",
            "datatableRowCellCard",
            "entityForm",
            "fluidGrid",
            "form",
            "formViewer",
            "gallery",
            "groupContainer",
            "layoutContainer",
            "pcfDataField",
            "typedDataCard",
        };

        private static readonly IReadOnlyDictionary<string, Func<string, DefaultRuleHelper, string>>
            PropertyDefaultScriptGetters = new Dictionary<string, Func<string, DefaultRuleHelper, string>>() {
            {
                "FillPortions", (templateName, ruleHelper) =>
                    _supportsNestedControls.Contains(templateName)
                        ? "1"
                        : "0"
            }, {
                "AlignInContainer", (templateName, ruleHelper) =>
                    _supportsNestedControls.Contains(templateName)
                        ? $"AlignInContainer.Stretch"
                        : $"AlignInContainer.SetByContainer"
            }, {
                "LayoutMinHeight", (templateName, ruleHelper) =>
                    _supportsNestedControls.Contains(templateName)
                        ? "32"
                        : (ruleHelper.TryGetDefaultRule("Height", out var defaultScript) ? defaultScript : "0")
            }, {
                "LayoutMinWidth", (templateName, ruleHelper) =>
                    _supportsNestedControls.Contains(templateName)
                        ? "32"
                        : (ruleHelper.TryGetDefaultRule("Width", out var defaultScript) ? defaultScript : "0")
            },
        };

        internal static bool IsResponsiveLayoutProperty(string propertyName)
        {
            return PropertyDefaultScriptGetters.ContainsKey(propertyName);
        }

        internal static bool TryGetDefaultValue(string propertyName, string template, DefaultRuleHelper defaultRuleHelper, out string defaultValue)
        {
            defaultValue = null;
            if (!PropertyDefaultScriptGetters.TryGetValue(propertyName, out  var defaultFunc))
            {
                return false;
            }

            defaultValue = defaultFunc.Invoke(template, defaultRuleHelper);
            return true;
        }

        // Key is property name, value is default script
        internal static IEnumerable<KeyValuePair<string, string>> GetDefaultValues(string templateName, DefaultRuleHelper defaultRuleHelper)
        {
            return PropertyDefaultScriptGetters.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.Invoke(templateName, defaultRuleHelper)));
        }
    }
}
