using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    // Responsible for handling dynamic properties and their default vaues
    internal static class DynamicProperties
    {
        internal static bool AddsChildDynamicProperties(string template)
        {
            return template == "groupContainer";
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

        private static readonly IReadOnlyDictionary<string, Func<string, string>>
            PropertyDefaultScriptGetters = new Dictionary<string, Func<string, string>>() {
            {
                "FillPortions", templateName =>
                    _supportsNestedControls.Contains(templateName)
                        ? "1"
                        : "0"
            }, {
                "AlignInContainer", templateName =>
                    _supportsNestedControls.Contains(templateName)
                        ? $"AlignInContainer.Stretch"
                        : $"AlignInContainer.SetByContainer"
            }, {
                "LayoutMinHeight", templateName =>
                    _supportsNestedControls.Contains(templateName)
                        ? "32"
                        : null
            }, {
                "LayoutMinWidth", templateName =>
                    _supportsNestedControls.Contains(templateName)
                        ? "32"
                        : null
            },
        };

        internal static bool TryGetDefaultValue(string propertyName, string template, out string defaultValue)
        {
            defaultValue = null;
            if (!PropertyDefaultScriptGetters.TryGetValue(propertyName, out var defaultFunc))
            {
                return false;
            }

            defaultValue = defaultFunc.Invoke(template);
            return true;
        }
    }
}
