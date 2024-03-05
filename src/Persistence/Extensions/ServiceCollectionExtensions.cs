// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// registers the MSAPP persistence services
    /// </summary>
    /// <param name="services">the services collection instance.</param>
    /// <param name="useDefaultTemplates">if true, registers the default templates (eg. 'text', 'button') on the templates store.</param>
    public static void AddPowerAppsPersistence(this IServiceCollection services, bool useDefaultTemplates = false)
    {
        services.AddSingleton<IMsappArchiveFactory, MsappArchiveFactory>();
        services.AddSingleton<IYamlSerializationFactory, YamlSerializationFactory>();
        services.AddSingleton<IControlFactory, ControlFactory>();

        services.AddSingleton<IControlTemplateStore, ControlTemplateStore>(ctx =>
        {
            var store = new ControlTemplateStore();

            AddMinimalTemplates(store);

            if (useDefaultTemplates)
                AddDefaultTemplates(store);

            store.DiscoverBuiltInTemplateTypes();

            return store;
        });
    }

    private static void AddMinimalTemplates(ControlTemplateStore store)
    {
        store.Add(new ControlTemplate { Name = "hostControl", DisplayName = "host", Id = "http://microsoft.com/appmagic/hostcontrol" });
        store.Add(new ControlTemplate { Name = "appInfo", DisplayName = "app", Id = "http://microsoft.com/appmagic/appinfo" });
        store.Add(new ControlTemplate { Name = "screen", Id = "http://microsoft.com/appmagic/screen" });
        store.Add(new ControlTemplate { Name = "component", Id = "http://microsoft.com/appmagic/Component" });
    }

    private static void AddDefaultTemplates(ControlTemplateStore store)
    {
        store.Add(new ControlTemplate { Name = "text", Id = "http://microsoft.com/appmagic/text" });
        store.Add(new ControlTemplate { Name = "button", Id = "http://microsoft.com/appmagic/button" });

        store.Add(new ControlTemplate { Name = "TextCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_TextCanvas" });
        store.Add(new ControlTemplate { Name = "ButtonCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas" });
    }
}
