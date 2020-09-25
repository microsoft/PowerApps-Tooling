// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.AppMagic.Authoring.Persistence
{
    internal class PaletteJson
    {
        public string value { get; set; } // A formula
        public string name { get; set; }
        public string type { get; set; } // a PA type, usually "c" for color. 
        public string phoneValue { get; set; }
    }

    internal class PropertyStyleJson
    {
        public string property { get; set; }
        public string value { get; set; } // Can use %% encoding. 
        public string phoneValue { get; set; }
    }

    internal class StylesJson
    {
        public string name { get; set; }
        public string controlTemplateName { get; set; }
        // public string CStyle { get; set; }
        public PropertyStyleJson[] propertyValuesMap { get; set; }        
    }


    internal class CustomThemeJson
    {
        public string name { get; set; }
        public PaletteJson[] palette { get; set; }
        public StylesJson[] styles { get; set; }
    }

    /// <summary>
    /// Schematic class for Themes.json
    /// </summary>
    internal class ThemesJson
    {
        public string CurrentTheme { get; set; }
        public CustomThemeJson[] CustomThemes { get; set; }
    }
}
