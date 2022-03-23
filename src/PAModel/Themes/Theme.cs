// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Encapsulate the ThemeJson.
    internal class Theme
    {
        // Outer key is stylename, inner key is property name, inner value is expression
        private readonly Dictionary<string, Dictionary<string, string>> _styles = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        
        public (ErrorContainer) Theme(ThemesJson themeJson)
        {
            var errors = new ErrorContainer();

            Contract.Assert(themeJson != null);

            var currentThemeName = themeJson.CurrentTheme;
            var namedThemes = themeJson.CustomThemes.ToDictionary(theme => theme.name);
            if (namedThemes.TryGetValue(currentThemeName, out var foundTheme))
            {
                LoadTheme(foundTheme, errors);
            }
            
        }


        private void LoadTheme(CustomThemeJson theme, ErrorContainer errors)
        {
            Dictionary<string, string> palleteRules = new Dictionary<string, string>();
            foreach (var item in theme.palette)
            {
                palleteRules[item.name] = item.value;
            }

            foreach (var style in theme.styles)
            {
                var styleName = style.name;
                foreach (var prop in style.propertyValuesMap)
                {
                    if(prop.value == ""){
                        errors.ValidationError($"Themes.json cannot contain a default script of empty string.");
                    }

                    var propName = prop.property;
                    var ruleValue = prop.value;

                    // TODO - share with logic in D:\dev\pa2\PowerApps-Client\src\Cloud\DocumentServer.Core\Document\Document\Theme\ControlStyle.cs
                    // Resolve %%, from palette.
                    {
                        var match = Regex.Match(ruleValue, "%Palette.([^%]+)%");
                        if (match.Success)
                        {
                            var group = match.Groups[1];

                            string resourceValue;
                            // Template may refer to a missing rule. 
                            if (palleteRules.TryGetValue(group.ToString(), out resourceValue))
                            {
                                ruleValue = ruleValue.Replace(match.Value, resourceValue);
                            }
                        }
                    }

                    ruleValue = ControlTemplateParser.UnescapeReservedName(ruleValue);

                    _styles.GetOrCreate(styleName).Add(propName, ruleValue);
                }
            }
        }

        public bool TryLookup(string styleName, string propertyName, out string defaultScript)
        {
            if (_styles.TryGetValue(styleName, out var styles))
            {
                return styles.TryGetValue(propertyName, out defaultScript);
            }
            defaultScript = null;
            return false;
        }

        // Returns style as copy of dictionary.
        // Must be copy since caller can mutate. 
        public Dictionary<string,string> GetStyle(string styleName)
        {
            var d = new Dictionary<string, string>();
            if (_styles.TryGetValue(styleName, out var styles))
            {
                d.AddRange(styles);
            }
            return d;
        }
    }
}
