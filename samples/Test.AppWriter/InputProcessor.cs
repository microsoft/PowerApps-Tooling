// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using MSAppGenerator;

namespace Test.AppWriter;

internal sealed class InputProcessor
{
    /// <summary>
    /// Validate that the provided filepath is accessible and not an existing file
    /// </summary>
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
    /// Function to bind to the Create command and call App Creation code
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "REVIEW")]
    private static void CreateFunction(bool interactive, string fullPathToMsApp, int numScreens, IList<string>? controlsinfo)
    {
        var creator = new AppCreator();

        try
        {
            creator.CreateMSApp(interactive, fullPathToMsApp, numScreens, controlsinfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Configures and returns the root command to process commandline arguments
    /// </summary>
    public static RootCommand GetRootCommand()
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
            CreateFunction(interactive, filepath!.FullName, numscreens, controls);
        }, interactiveOption, filePathOption, numScreensOption, controlsOptions);

        rootCommand.AddCommand(createCommand);

        return rootCommand;
    }
}
