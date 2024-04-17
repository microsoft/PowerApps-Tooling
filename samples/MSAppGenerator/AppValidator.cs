// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
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

    private IMsappArchive? GetAppFromFile(string filePath)
    {
        try
        {
            var msapp = _serviceProvider.GetRequiredService<IMsappArchiveFactory>().Update(filePath, overwriteOnSave: true);
            if (!string.IsNullOrEmpty(msapp.App.Name) && msapp.App.Screens.Count >= 1)
            {
                return msapp;
            }
        }
        catch (NullReferenceException ex) {
            Console.WriteLine(ex.Message);
        }
        return null;
    }

    public bool ValidateMSApp(string filePath, string savePath)
    {
        var msapp = GetAppFromFile(filePath);

        if (msapp == null)
        {
            return false;
        }

        msapp.SaveAs(savePath);
        return true;
    }
}
