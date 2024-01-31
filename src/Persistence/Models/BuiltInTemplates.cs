// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public static class BuiltInTemplates
{
    public const string AppInfo = "http://microsoft.com/appmagic/appinfo";
    public const string HostControl = "http://microsoft.com/appmagic/hostcontrol";
    public const string Screen = "http://microsoft.com/appmagic/screen";
    public const string Button = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas";
    public const string Text = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_TextCanvas";

    public static readonly IReadOnlyDictionary<string, Type> ShortNameToType;
    public static readonly IReadOnlyDictionary<string, Type> TemplateToType;
    public static readonly IReadOnlyDictionary<Type, string> TypeToShortName;

#pragma warning disable CA1810 // Initialize reference type static fields inline
    static BuiltInTemplates()
#pragma warning restore CA1810
    {
        var shortNameToType = new Dictionary<string, Type>();
        var templateToType = new Dictionary<string, Type>();
        var typeToShortName = new Dictionary<Type, string>();

        var types = typeof(Control).Assembly.DefinedTypes;
        foreach (var type in types)
        {
            // Ignore anything that isn't a control.
            if (!type.IsAssignableTo(typeof(Control)))
                continue;

            if (type.GetCustomAttributes(true).FirstOrDefault(a => a is FirstClassAttribute) is FirstClassAttribute firstClassAttribute)
            {
                shortNameToType.Add(firstClassAttribute.ShortName, type);
                typeToShortName.Add(type, firstClassAttribute.ShortName);
                templateToType.Add(firstClassAttribute.TemplateUri, type);
            }
        }

        ShortNameToType = shortNameToType;
        TemplateToType = templateToType;
        TypeToShortName = typeToShortName;
    }
}
