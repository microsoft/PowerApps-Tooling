// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using MSAppGenerator;

namespace Test.AppWriter;

internal class Program
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

    private static bool ValidateFilePath(string filePath, out string error)
    {
        error = string.Empty;
        if (!File.Exists(filePath))
            return true;

        Console.WriteLine($"Warning: File '{filePath}' already exists");
        Console.Write("Overwrite? ([y]/n) - enter for yes: ");

        var input = Console.ReadKey();
        Console.WriteLine();
        if (input.Key == ConsoleKey.Enter || input.Key == ConsoleKey.Y)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (IOException ex)
            {
                error = $"Error: {ex.Message}";
                return false;
            }
            return true;
        }

        error = $"Unable to overwrite file";
        return false;
    }

    /// <summary>
    /// Attempt to do specified app creation
    /// </summary>
    private static void CreateMSApp(bool interactive, string fullPathToMsApp, int numScreens, IList<string>? controlsinfo)
    {
        // Setup services for creating MSApp representation
        var provider = ConfigureServiceProvider();

        // Create a new empty MSApp
        using var msapp = provider.GetRequiredService<IMsappArchiveFactory>().Create(fullPathToMsApp);

        // Select Generator based off specified mode
        var generator = provider.GetRequiredService<IAppGeneratorFactory>().Create(interactive);

        // Generate the app
        msapp.App = generator.GenerateApp(Path.GetFileNameWithoutExtension(fullPathToMsApp),
                numScreens, controlsinfo);

        // Output the MSApp to the path provided
        msapp.Save();
        Console.WriteLine("Success!  MSApp generated and saved to the provided path");
    }

    private static void Main(string[] args)
    {
        var interactiveOption = new Option<bool>(
            name: "--interactive",
            description: "Enables interactive mode for MSApp creation",
            getDefaultValue: () => false // If --interactive is not specified, default to argument based creation
            );
        var filePathOption = new Option<FileInfo?>(
            name: "--filepath",
            description: "(string) The path where the msapp file should be generated, including filename and extension",
            parseArgument: result =>
            {
                var filePath = result.Tokens.Single().Value;

                if (ValidateFilePath(filePath, out var error))
                    return new FileInfo(filePath);

                result.ErrorMessage = error;
                return null;
            }
            )
        { IsRequired = true };
        var numScreensOption = new Option<int>(
            name: "--numscreens",
            description: "(integer) The number of screens to generate in the App",
            getDefaultValue: () => 1
            );
        numScreensOption.AddValidator(result =>
        {
            if (result.GetValueForOption(numScreensOption) < 0)
            {
                result.ErrorMessage = "Number of screens must be greater than 0";
            }
        });
        var controlsOptions = new Option<IList<string>>(
            name: "--controls",
            description: "(list of string) A list of control templates (i.e. Button Label [Template]...)")
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
