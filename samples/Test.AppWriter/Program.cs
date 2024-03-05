// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Test.AppWriter;

internal class Program
{
    //    private struct ControlsInfo
    //    {
    //        public int numScreens;
    //        public string[] controls;
    //    };

    // Configures default services for generating the MSApp representation
    private static IServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPowerAppsPersistence(true);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }

    //private static string? ValidateFilePath()
    //{
    //    try
    //    {
    //        var fileCheck = new FileInfo(msAppPath);
    //        if (File.Exists(msAppPath)) // Overwrite
    //        {
    //            Console.WriteLine("Warning: File already exists");
    //            Console.WriteLine("Provided path: " + msAppPath);
    //            Console.Write("    Overwrite? (y / n): ");
    //            var input = Console.ReadLine();
    //            if (input?.ToLower()[0] == 'y') File.Delete(msAppPath);
    //            else
    //            {
    //                Console.WriteLine("Exiting");
    //                return;
    //            }
    //        }
    //        else
    //        {
    //            Console.WriteLine("Creating MSApp at filepath: " + fileCheck.FullName);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine("Filepath invalid: " + ex);
    //        return;
    //    }
    //}

    private static void CreateMSApp(string fullPathToMsApp, int numScreens)
    {
        // Setup services for creating MSApp representation
        var provider = ConfigureServiceProvider();

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        // Add a basic example app (note: this will be replaced with interactive process)
        msapp.App = ExampleAppGenerator.GetExampleApp(provider, Path.GetFileNameWithoutExtension(fullPathToMsApp), numScreens);

        // Output the MSApp to the path provided
        msapp.Save();
        Console.WriteLine("Success!  MSApp generated and saved to the provided path");
    }

    // Attempt to do specified app creation
    private static void CreateMSApp(string fullPathToMsApp, int numScreens, string[] controlsinfo)
    {
        // Setup services for creating MSApp representation
        var provider = ConfigureServiceProvider();

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        msapp.App = ExampleAppGenerator.CreateApp(provider, Path.GetFileNameWithoutExtension(fullPathToMsApp), numScreens, ExampleAppGenerator.ParseControlsInfo(controlsinfo));

        // Output the MSApp to the path provided
        msapp.Save();
        Console.WriteLine("Success!  MSApp generated and saved to the provided path");
    }

    //private static App InteractiveGenerator()
    //{
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
    //}

    private static void Main(string[] args)
    {
        //var = new Option<>(
        //    name: "",
        //    description: ""
        //    );
        //var interactiveOption = new Option<bool>(
        //    name: "--interactive",
        //    description: "Enables interactive mode for MSApp creation"
        //    );
        var filePathOption = new Option<FileInfo?>(
            name: "--filepath",
            description: "(string) The path where the msapp file should be generated, including filename and extension",
            isDefault: true,
            parseArgument: result =>
            {
                if (result.Tokens.Count == 0)
                {
                    result.ErrorMessage = "Must provide a file path.";
                    return null;
                    //Console.WriteLine("Using default out path: ");
                    //return new FileInfo("appname.msapp");
                }
#pragma warning disable IDE0007 // Use implicit type
                string? filePath = result.Tokens.Single().Value;
#pragma warning restore IDE0007 // Use implicit type
                if (File.Exists(filePath)) // TODO: Move this validation after input is handled
                {
                    Console.WriteLine("Warning: File already exists");
                    Console.WriteLine("Provided path: " + filePath);
                    Console.Write("    Overwrite? (y / n): ");
                    var input = Console.ReadLine();
                    if (input?.ToLower()[0] == 'y')
                    {
                        File.Delete(filePath);
                        return new FileInfo(filePath);
                    }
                    else
                    {
                        result.ErrorMessage = "File already exists and overwrite declined, exiting";
                        return null;
                    }
                }
                else
                {
                    return new FileInfo(filePath);
                }
            }
            );
        var numScreensOption = new Option<int>(
            name: "--numscreens",
            description: "(integer) The number of screens to generate in the App",
            isDefault: true,
            parseArgument: result =>
            {
                if (!result.Tokens.Any())
                {
                    result.ErrorMessage = "Must provide a value for number of screens";
                    return 0; // Ignored.
                }
                else if (int.TryParse(result.Tokens.Single().Value, out var number))
                {
                    if (number < 0)
                    {
                        result.ErrorMessage = "Number of screens must not be a negative number.";
                    }
                    return number;
                }
                else
                {
                    result.ErrorMessage = "Invalid value, unable to convert to integer.";
                    return 0; // Ignored.
                }
            }
            );
        var controlsOptions = new Option<string[]>(
            name: "--controls",
            description: "(list of string) A list of control name and template pairs (i.e. mybutton Button labelname Label [controlname Template]...)")
        { AllowMultipleArgumentsPerToken = true };


        var rootCommand = new RootCommand("Test Writer for MSApp files.");
        var createCommand = new Command("create", "Create a new MSApp at the specified path.")
        {
            filePathOption,
            numScreensOption,
            controlsOptions
        };

        rootCommand.AddCommand(createCommand);

        createCommand.SetHandler((filepath, numscreens, controls) =>
        {
            CreateMSApp(filepath!.FullName, numscreens, controls);
        }, filePathOption, numScreensOption, controlsOptions);

        rootCommand.Invoke(args);
    }
}
