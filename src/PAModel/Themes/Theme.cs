// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.PowerPlatform.Formulas.Tools;

// Encapsulate the ThemeJson.
internal class Theme
{
    // Outer key is style name, inner key is property name, inner value is expression
    private readonly Dictionary<string, Dictionary<string, string>> _styles = new(StringComparer.OrdinalIgnoreCase);

    public Theme(ThemesJson themeJson)
    {
        Contract.Assert(themeJson != null);

        var currentThemeName = themeJson.CurrentTheme;
        var namedThemes = themeJson.CustomThemes.ToDictionary(theme => theme.name);
        if (namedThemes.TryGetValue(currentThemeName, out var foundTheme))
        {
            LoadTheme(foundTheme);
        }

    }


    private void LoadTheme(CustomThemeJson theme)
    {
        var palleteRules = new Dictionary<string, string>();
        foreach (var item in theme.palette)
        {
            palleteRules[item.name] = item.value;
        }

        foreach (var style in theme.styles)
        {
            var styleName = style.name;
            foreach (var prop in style.propertyValuesMap)
            {
                var propName = prop.property;
                var ruleValue = prop.value;

                // TODO - share with logic in D:\dev\pa2\PowerApps-Client\src\Cloud\DocumentServer.Core\Document\Document\Theme\ControlStyle.cs
                // Resolve %%, from palette.
                {
                    var match = Regex.Match(ruleValue, "%Palette.([^%]+)%");
                    if (match.Success)
                    {
                        var group = match.Groups[1];

                        // Template may refer to a missing rule. 
                        if (palleteRules.TryGetValue(group.ToString(), out var resourceValue))
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
    public Dictionary<string, string> GetStyle(string styleName)
    {
        var d = new Dictionary<string, string>();
        if (_styles.TryGetValue(styleName, out var styles))
        {
            d.AddRange(styles);
        }
        return d;
    }
}
