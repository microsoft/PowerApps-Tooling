// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.AppMagic.Authoring.Persistence;


namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;

internal sealed class ControlProperty(string name, string defaultVal, string phoneDefault, string webDefault)
{
    public string Name { get; } = name;
    public string DefaultValue { get; } = defaultVal;
    public string PhoneDefaultValue { get; } = phoneDefault;
    public string WebDefaultValue { get; } = webDefault;

    public string GetDefaultValue(AppType type)
    {
        if (type == AppType.Phone && PhoneDefaultValue != null)
            return PhoneDefaultValue;
        if (type == AppType.Web && WebDefaultValue != null)
            return WebDefaultValue;

        return DefaultValue;
    }
}
