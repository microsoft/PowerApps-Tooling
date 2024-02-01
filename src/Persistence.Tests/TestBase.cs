// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests;

public class TestBase
{
    public required TestContext TestContext { get; set; }

    public static IServiceProvider ServiceProvider { get; set; }

    static TestBase()
    {
        ServiceProvider = BuildServiceProvider();
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

        return services.BuildServiceProvider();
    }
}
