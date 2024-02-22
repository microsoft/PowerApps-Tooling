// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Test.AppWriter;

internal class Program
{
    // Configures default services for generating the MSApp representation
    private static IServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPowerAppsPersistence(true);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    private static void Main(string fullPathToMsApp)
    {
        // Setup services for creating MSApp representation
        var provider = ConfigureServiceProvider();

        //var fullPathToMsApp = args.Length > 0 ? args[0] : null;
        //if (fullPathToMsApp == null)
        //{
        //    Console.WriteLine("No args provided, using default file path");
        //    fullPathToMsApp = Path.Combine(Directory.GetCurrentDirectory(), "CanvasApp.msapp");
        //    Console.WriteLine(fullPathToMsApp);
        //}

        if (File.Exists(fullPathToMsApp)) // Overwrite
        {
            Console.WriteLine("Warning: File already exists;  Overwrite? (y / n)");
            var input = Console.ReadLine();
            if (input?.ToLower()[0] == 'y') File.Delete(fullPathToMsApp);
        }

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);


        // Add a basic example app (note: this will be replaced with interactive process)
        msapp.App = ExampleAppGenerator.GetExampleApp(provider, Path.GetFileNameWithoutExtension(fullPathToMsApp));

        // Output the MSApp to the path provided
        msapp.Save();
    }
}
