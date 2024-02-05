// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace Persistence.Tests;

public class TestBase
{
    public required TestContext TestContext { get; set; }

    public static IServiceProvider ServiceProvider { get; set; }

    public IControlTemplateStore ControlTemplateStore { get; private set; }

    public IMsappArchiveFactory MsappArchiveFactory { get; private set; }

    public IControlFactory ControlFactory { get; private set; }

    static TestBase()
    {
        ServiceProvider = BuildServiceProvider();
    }

    public TestBase()
    {
        // Request commonly used services
        ControlTemplateStore = ServiceProvider.GetRequiredService<IControlTemplateStore>();
        MsappArchiveFactory = ServiceProvider.GetRequiredService<IMsappArchiveFactory>();
        ControlFactory = ServiceProvider.GetRequiredService<IControlFactory>();
    }

    public static IServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = ConfigureServices(serviceCollection);

        return serviceProvider;
    }

    private static IServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.AddPowerAppsPersistence(useDefaultTemplates: true);

        return services.BuildServiceProvider();
    }
}
