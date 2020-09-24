// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;


namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    internal sealed class ControlProperty
    {
        public string Name { get; }
        public string DefaultValue { get; }
        public string PhoneDefaultValue { get; }
        public string WebDefaultValue { get; }

        public ControlProperty(string name, string defaultVal, string phoneDefault, string webDefault)
        {
            Name = name;
            DefaultValue = defaultVal;
            PhoneDefaultValue = phoneDefault;
            WebDefaultValue = webDefault;
        }

        public string GetDefaultValue(AppType type)
        {
            if (type == AppType.Phone && PhoneDefaultValue != null)
                return PhoneDefaultValue;
            if (type == AppType.Web && WebDefaultValue != null)
                return WebDefaultValue;

            return DefaultValue;
        }
    }
}
