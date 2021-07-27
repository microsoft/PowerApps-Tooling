// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class ThemeChange : IDelta
    {
        private ThemesJson _theme;

        public ThemeChange(ThemesJson theme)
        {
            _theme = theme;
        }

        // As a starting point, overwrite the theme as a whole.
        public void Apply(CanvasDocument document)
        {
            document._themes = _theme;
        }
    }
}
