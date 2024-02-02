// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests;

public class TestBase
{
    public required TestContext TestContext { get; set; }

    public static IServiceProvider ServiceProvider { get; set; }

    public IControlTemplateStore ControlTemplateStore { get; private set; }

    static TestBase()
    {
        ServiceProvider = BuildServiceProvider();
    }

    public TestBase()
    {
        ControlTemplateStore = ServiceProvider.GetRequiredService<IControlTemplateStore>();
    }

    public static IServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = ConfigureServices(serviceCollection);

        return serviceProvider;
    }

    private static IServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IYamlSerializationFactory, YamlSerializationFactory>();
        services.AddSingleton<IControlTemplateStore, ControlTemplateStore>(s =>
        {
            var store = new ControlTemplateStore();

            store.Add(new ControlTemplate { Name = "TextCanvas", Uri = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_TextCanvas" });
            store.Add(new ControlTemplate { Name = "ButtonCanvas", Uri = "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas" });

            return store;
        });

        return services.BuildServiceProvider();
    }
}
