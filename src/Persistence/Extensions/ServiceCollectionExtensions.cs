// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPowerAppsPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IYamlSerializationFactory, YamlSerializationFactory>();
        services.AddSingleton<IControlTemplateStore, ControlTemplateStore>(LoadControlTemplateStore);
    }

    /// <summary>
    /// Loads default control templates
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static ControlTemplateStore LoadControlTemplateStore(IServiceProvider serviceProvider)
    {
        var store = new ControlTemplateStore();

        store.Add(new ControlTemplate { Name = "TextCanvas", Uri = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_TextCanvas" });
        store.Add(new ControlTemplate { Name = "ButtonCanvas", Uri = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas" });

        return store;

    }
}
