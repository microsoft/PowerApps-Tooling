// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;


namespace Persistence.YamlValidator.Tests;
public abstract class TestBase : VSTestBase
{
    public static IServiceProvider ServiceProvider { get; set; }
    public IValidatorFactory ValidatorFactory { get; private set; }

    static TestBase()
    {
        ServiceProvider = BuildServiceProvider();
    }

    public TestBase()
    {
        ValidatorFactory = ServiceProvider.GetRequiredService<IValidatorFactory>();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = ConfigureServices(serviceCollection);

        return serviceProvider;
    }

    private static ServiceProvider ConfigureServices(IServiceCollection services)
    {
        services.AddPowerAppsPersistenceYamlValidator();

        return services.BuildServiceProvider();
    }

}
