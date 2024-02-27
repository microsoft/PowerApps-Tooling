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

    private static void Main(string msAppPath, int numScreens)
    {
        //var msAppPath = args.Length > 0 ? args[0] : null;
        //if (msAppPath == null)
        //{
        //    Console.WriteLine("No args provided, using default file path");
        //    msAppPath = Path.Combine(Directory.GetCurrentDirectory(), "CanvasApp.msapp");
        //    Console.WriteLine(msAppPath);
        //}

        try
        {
            var fileCheck = new FileInfo(msAppPath);
            if (File.Exists(msAppPath)) // Overwrite
            {
                Console.WriteLine("Warning: File already exists");
                Console.WriteLine("Provided path: " + msAppPath);
                Console.Write("    Overwrite? (y / n): ");
                var input = Console.ReadLine();
                if (input?.ToLower()[0] == 'y') File.Delete(msAppPath);
                else
                {
                    Console.WriteLine("Exiting");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Creating MSApp at filepath: " + fileCheck.FullName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Filepath invalid: " + ex);
            return;
        }

        // Setup services for creating MSApp representation
        var provider = ConfigureServiceProvider();

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(msAppPath);

        // do interactive session
        //var inProgress = true;
        //while (inProgress)
        //{
        //    Console.Write("Create new Screen? (y/n): ");
        //    var input = Console.ReadLine();
        //    if (input?.ToLower()[0] == 'y') { } // do custom controls creation
        //    else
        //    {
        //        Console.Write("Create new Screen? (y/n): ");
        //    }
        //}

        // Add a basic example app (note: this will be replaced with interactive process)
        msapp.App = ExampleAppGenerator.GetExampleApp(provider, Path.GetFileNameWithoutExtension(msAppPath), numScreens);

        // Output the MSApp to the path provided
        msapp.Save();
        Console.WriteLine("Success!  MSApp generated and saved to the provided path");
    }
}
