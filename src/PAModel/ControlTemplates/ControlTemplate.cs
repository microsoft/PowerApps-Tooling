// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;


namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    internal sealed class ControlTemplate
    {
        public string Name { get; }
        public string Version { get; }
        public string Id { get; }

        // Property Name -> Default Expression
        public Dictionary<string, string> InputDefaults { get; }

        public Dictionary<string, Dictionary<string, string>> VariantDefaultValues { get; }

        public ControlTemplate(string name, string version, string id)
        {
            Name = name;
            Version = version;
            Id = id;
            InputDefaults = new Dictionary<string, string>();
            VariantDefaultValues = new Dictionary<string, Dictionary<string, string>>();
        }
    }
}
