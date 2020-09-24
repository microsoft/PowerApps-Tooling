// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    internal class CommonControlProperties
    {
        private readonly Dictionary<string, ControlProperty> _properties;
        private static readonly Lazy<CommonControlProperties> _lazyCommonControlProperties = new Lazy<CommonControlProperties>(() => new CommonControlProperties());
        private static readonly string _fileName = "Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates.commonStyleProperties.xml";

        public static CommonControlProperties Instance => _lazyCommonControlProperties.Value;

        private CommonControlProperties()
        {
            _properties = new Dictionary<string, ControlProperty>();
            Load();
        }

        private void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(_fileName);
            var commonProps = XDocument.Load(stream);
            if (commonProps == null)
                return;

            foreach (var property in commonProps.Root.Elements(ControlMetadataXNames.PropertyTag))
            {
                ControlProperty controlProperty = ControlTemplateParser.ParseProperty(property);
                if (controlProperty == null)
                    continue;
                _properties.Add(controlProperty.Name, controlProperty);
            }
        }

        // Null if property not found
        public string GetDefaultValue(string propertyName, AppType type)
        {
            if (!_properties.TryGetValue(propertyName, out var controlProperty))
                return null;

            return controlProperty.GetDefaultValue(type);
        }
    }
}
