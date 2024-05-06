// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace MSAppGenerator;

public class AppCreator
{
    /// <summary>
    /// Configures default services for generating the MSApp representation
    /// </summary>
    private static ServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPowerAppsPersistence(true);
        serviceCollection.AddSingleton<IAppGeneratorFactory, AppGeneratorFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    private IServiceProvider _serviceProvider { get; set; }

    public AppCreator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public AppCreator()
    {
        _serviceProvider = ConfigureServiceProvider();
    }

    /// <summary>
    /// Attempt to do specified app creation
    /// </summary>
    public void CreateMSApp(bool interactive, string fullPathToMsApp, int numScreens, IList<string>? controlsinfo)
    {
        // Create a new empty MSApp
        using var msapp = _serviceProvider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        // Select Generator based off specified mode
        var generator = _serviceProvider.GetRequiredService<IAppGeneratorFactory>().Create(interactive);

        // Generate the app
        msapp.App = generator.GenerateApp(numScreens, controlsinfo);

        // Output the MSApp to the path provided
        msapp.Save();
    }
}
