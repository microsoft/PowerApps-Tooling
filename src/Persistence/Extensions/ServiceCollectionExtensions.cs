// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPowerAppsPersistence(this IServiceCollection services, bool useDefaultTemplates = false)
    {
        services.AddSingleton<IMsappArchiveFactory, MsappArchiveFactory>();
        services.AddSingleton<IYamlSerializationFactory, YamlSerializationFactory>();
        services.AddSingleton<IControlFactory, ControlFactory>();

        if (useDefaultTemplates)
            services.AddSingleton<IControlTemplateStore, ControlTemplateStore>(WithDefaultTemplates);
        else
            services.AddSingleton<IControlTemplateStore, ControlTemplateStore>(WithMinimalTemplates);
    }

    /// <summary>
    /// Adds default templates to the store.
    /// </summary>
    public static ControlTemplateStore WithDefaultTemplates(IServiceProvider _)
    {
        var store = new ControlTemplateStore();

        store.Add(new ControlTemplate { Name = "host", Id = "http://microsoft.com/appmagic/hostcontrol" });
        store.Add(new ControlTemplate { Name = "hostControl", Id = "http://microsoft.com/appmagic/hostcontrol" });
        store.Add(new ControlTemplate { Name = "app", Id = "http://microsoft.com/appmagic/appinfo" });
        store.Add(new ControlTemplate { Name = "appInfo", Id = "http://microsoft.com/appmagic/appinfo" });
        store.Add(new ControlTemplate { Name = "screen", Id = "http://microsoft.com/appmagic/screen" });

        store.Add(new ControlTemplate { Name = "text", Id = "http://microsoft.com/appmagic/text" });
        store.Add(new ControlTemplate { Name = "button", Id = "http://microsoft.com/appmagic/button" });

        store.Add(new ControlTemplate { Name = "TextCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_TextCanvas" });
        store.Add(new ControlTemplate { Name = "ButtonCanvas", Id = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas" });

        store.DiscoverBuiltInTemplateTypes();

        return store;
    }

    public static ControlTemplateStore WithMinimalTemplates(IServiceProvider _)
    {
        var store = new ControlTemplateStore();

        store.Add(new ControlTemplate { Name = "host", Id = "http://microsoft.com/appmagic/hostcontrol" });
        store.Add(new ControlTemplate { Name = "app", Id = "http://microsoft.com/appmagic/appinfo" });
        store.Add(new ControlTemplate { Name = "screen", Id = "http://microsoft.com/appmagic/screen" });

        store.DiscoverBuiltInTemplateTypes();

        return store;
    }
}
