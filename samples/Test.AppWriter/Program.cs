// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
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
    private static void CreateMSApp(bool interactive, string fullPathToMsApp, int numScreens, string[] controlsinfo)
    {
        // Setup services for creating MSApp representation
        var provider = ConfigureServiceProvider();

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        if (interactive)
        {
            msapp.App = InteractiveAppGenerator.GenerateApp(provider, Path.GetFileNameWithoutExtension(fullPathToMsApp));
        }
        else
        {
            msapp.App = ExampleAppGenerator.CreateApp(provider, Path.GetFileNameWithoutExtension(fullPathToMsApp), numScreens, ExampleAppGenerator.ParseControlsInfo(controlsinfo));
        }

        // Output the MSApp to the path provided
        msapp.Save();
        Console.WriteLine("Success!  MSApp generated and saved to the provided path");
    }

    private static void Main(string[] args)
    {
        //var = new Option<>(
        //    name: "",
        //    description: ""
        //    );
        var interactiveOption = new Option<bool>(
            name: "--interactive",
            description: "Enables interactive mode for MSApp creation",
            getDefaultValue: () => true
            );
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

                var filepath = result.Tokens.Single().Value;
                try
                {
                    var fileinfo = new FileInfo(filepath);
                    return fileinfo;
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = "Invalid filepath: " + ex.ToString();
                    return null;
                }
            }
            );
        var numScreensOption = new Option<int>(
            name: "--numscreens",
            description: "(integer) The number of screens to generate in the App",
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
        //var screenNamesOption = new Option<string>(
        //    name: "--screennames"
        //    );
        var controlsOptions = new Option<string[]>(
            name: "--controls",
            description: "(list of string) A list of control name and template pairs (i.e. mybutton Button labelname Label [controlname Template]...)")
        { AllowMultipleArgumentsPerToken = true };

        var rootCommand = new RootCommand("Test Writer for MSApp files.");
        var createCommand = new Command("create", "Create a new MSApp at the specified path.")
        {
            interactiveOption,
            filePathOption,
            numScreensOption,
            controlsOptions
        };

        createCommand.SetHandler((interactive, filepath, numscreens, controls) =>
        {
            CreateMSApp(interactive, filepath!.FullName, numscreens, controls);
        }, interactiveOption, filePathOption, numScreensOption, controlsOptions);

        rootCommand.AddCommand(createCommand);

        rootCommand.Invoke(args);
    }
}
