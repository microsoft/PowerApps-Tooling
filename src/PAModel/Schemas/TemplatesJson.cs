// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    internal class TemplatesJson
    {
        internal class TemplateJson
        {
            /// <summary>
            /// Template Name, matches ControlInfoJson.Template.Name
            /// </summary>
            public string Name;

            /// <summary>
            /// Template Name, matches ControlInfoJson.Template.Name
            /// </summary>
            public string Version;

            /// <summary>
            /// Stringified XML control template, from _oam.xml files in PowerApps Codebase
            /// </summary>
            public string Template;
        }

        public TemplateJson[] UsedTemplates;
    }
}
