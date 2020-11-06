// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
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
            public string Name { get; set; }

            /// <summary>
            /// Template Version, matches ControlInfoJson.Template.version
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            /// Stringified XML control template, from _oam.xml files in PowerApps Codebase
            /// </summary>
            public string Template { get; set; }
        }

        public TemplateJson[] UsedTemplates { get; set; }
        public TemplateMetadataJson[] ComponentTemplates { get; set; }
    }
}
