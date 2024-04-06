// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

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

    private static IServiceProvider BuildServiceProvider()
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

    public static IYamlDeserializer CreateDeserializer(bool isControlIdentifiers = false, bool isTextFirst = false)
    {
        return ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer
        (
            new YamlSerializationOptions
            {
                IsTextFirst = isTextFirst,
                IsControlIdentifiers = isControlIdentifiers
            }
        );
    }

    public static IYamlSerializer CreateSerializer(bool isControlIdentifiers = false, bool isTextFirst = false)
    {
        return ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer
        (
            new YamlSerializationOptions
            {
                IsTextFirst = isTextFirst,
                IsControlIdentifiers = isControlIdentifiers
            }
        );
    }

    public static string GetTestFilePath(string path, bool isControlIdentifiers = false)
    {
        return string.Format(path, isControlIdentifiers ? "-CI" : string.Empty);
    }
}
