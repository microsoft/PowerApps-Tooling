// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    internal class ControlTemplateParser
    {
        internal static bool TryParseTemplate(string templateString, AppType type, out ControlTemplate template, out string name)
        {
            template = null;
            name = string.Empty;
            try
            {
                var manifest = XDocument.Parse(templateString);
                if (manifest == null)
                    return false;
                var widget = manifest.Root;
                if (widget.Name != ControlMetadataXNames.WidgetTag)
                    return false;

                name = widget.Attribute(ControlMetadataXNames.NameAttribute).Value;
                var id = widget.Attribute(ControlMetadataXNames.IdAttribute).Value;
                var version = widget.Attribute(ControlMetadataXNames.VersionAttribute).Value;

                template = new ControlTemplate(name, version, id);

                var properties = widget.Element(ControlMetadataXNames.PropertiesTag);
                if (properties != null)
                {
                    foreach (var prop in properties.Elements(ControlMetadataXNames.PropertyTag))
                    {
                        if (!AddPropertyDefault(prop, type, template))
                            return false;
                    }
                }

                // Parse the include properties and add them to control template
                var includeProperties = widget.Element(ControlMetadataXNames.IncludePropertiesTag);
                if (includeProperties != null)
                {
                    foreach (var includeProperty in includeProperties.Elements(ControlMetadataXNames.IncludePropertyTag))
                    {
                        AddIncludePropertyDefault(includeProperty, type, template);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool AddPropertyDefault(XElement prop, AppType type, ControlTemplate template)
        {
            var ctrlProp = ParseProperty(prop);
            if (ctrlProp == null)
                return false;
            template.InputDefaults.Add(ctrlProp.Name, ctrlProp.GetDefaultValue(type));
            return true;
        }

        private static void AddIncludePropertyDefault(XElement includeProperty, AppType type, ControlTemplate template)
        {
            var propertyName = includeProperty.Attribute(ControlMetadataXNames.NameAttribute).Value;
            // Explicitly defined props overwrite common props
            if (template.InputDefaults.ContainsKey(propertyName))
                return;

            var defaultOverride = includeProperty.Attribute(ControlMetadataXNames.DefaultValueAttribute);
            if (defaultOverride != null)
                template.InputDefaults.Add(propertyName, defaultOverride.Value);
            else 
                template.InputDefaults.Add(propertyName, CommonControlProperties.Instance.GetDefaultValue(propertyName, type));
        }


        internal static ControlProperty ParseProperty(XElement property)
        {
            var nameAttr = property.Attribute(ControlMetadataXNames.NameAttribute);
            if (nameAttr == null)
                return null;

            string defaultValue = null;
            string phoneDefaultValue = null;
            string webDefaultValue = null;

            var defaultValueAttrib = property.Attribute(ControlMetadataXNames.DefaultValueAttribute);
            if (defaultValueAttrib != null)
                defaultValue = defaultValueAttrib.Value;

            var phoneDefaultValueAttrib = property.Attribute(ControlMetadataXNames.PhoneDefaultValueAttribute);
            if (phoneDefaultValueAttrib != null)
                phoneDefaultValue = phoneDefaultValueAttrib.Value;

            var webDefaultValueAttrib = property.Attribute(ControlMetadataXNames.WebDefaultValueAttribute);
            if (webDefaultValueAttrib != null)
                webDefaultValue = webDefaultValueAttrib.Value;

            return new ControlProperty(nameAttr.Value, defaultValue, phoneDefaultValue, webDefaultValue);
        }

    }
}
