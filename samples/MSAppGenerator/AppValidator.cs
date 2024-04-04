// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace MSAppGenerator;

public class AppValidator
{
    /// <summary>
    /// Configures default services for generating the MSApp representation
    /// </summary>
    private static IServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPowerAppsPersistence(true);
        serviceCollection.AddSingleton<IAppGeneratorFactory, AppGeneratorFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    IServiceProvider _serviceProvider { get; set; }

    public AppValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public AppValidator()
    {
        _serviceProvider = ConfigureServiceProvider();
    }

    public IMsappArchive? GetAppFromFile(string filePath)
    {
        try
        {
            using var msapp = _serviceProvider.GetRequiredService<IMsappArchiveFactory>().Open(filePath);
            if (!string.IsNullOrEmpty(msapp.App.Name) && msapp.App.Screens.Count >= 1)
            {
                return msapp;
            }
        }
        catch (NullReferenceException ex) { }
        return null;
    }

    //public App ValidateMSApp(IMsappArchive app)
    //{

    //}
}
