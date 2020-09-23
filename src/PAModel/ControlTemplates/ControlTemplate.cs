
using System;
using System.Collections.Generic;


namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    public sealed class ControlTemplate
    {
        public string Name { get; }
        public string Version { get; }
        public string Id { get; }
        public Dictionary<string, string> InputDefaults { get; }

        public ControlTemplate(string name, string version, string id)
        {
            Name = name;
            Version = version;
            Id = id;
            InputDefaults = new Dictionary<string, string>();
        }

    }
}
