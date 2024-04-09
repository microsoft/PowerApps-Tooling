// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using MSAppGenerator;

namespace Test.AppWriter;

internal class InputProcessor
{
    /// <summary>
    /// Validate that the provided filepath is accessible and not an existing file
    /// </summary>
    private static bool ValidateFilePath(string filePath, out string error, bool isFolder = false)
    {
        error = string.Empty;

        var dirCheck = false;
        if (isFolder)
        {
            dirCheck &= File.GetAttributes(filePath).HasFlag(FileAttributes.Directory);
        }
        if (!File.Exists(filePath) || dirCheck)
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
    /// Roundtrips all MSApps in a folder, will generate a report at the outpath 
    /// </summary>
    private static void ValidateFunction(string filePath, string outPath, int numPasses)
    {
        var validator = new AppValidator();

        var msapp = validator.GetAppFromFile(filePath);

        if (msapp != null)
        {
            var msapp = validator.GetAppFromFile(file.FullName);

            if (msapp != null)
            {
                msapp.SaveAs(outPath + '\\' + file.Name);
            }
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
            description: "(string) The desired filepath for the msapp file, including filename and extension",
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
        var folderPathOption = new Option<FileInfo?>(
            name: "--folderpath",
            description: "(string) The desired path to folder containing MSAPPs",
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
        var outPathOption = new Option<FileInfo?>(
            name: "--outpath",
            description: "(string) The path where results of validation should be output",
            parseArgument: result =>
            {
                var filePath = result.Tokens.Single().Value;

                if (ValidateFilePath(filePath, out var error))
                    return new FileInfo(filePath);

                result.ErrorMessage = error;
                return null;
            }
            );
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
        var numPassesOption = new Option<int>(
            name: "--numpasses",
            description: "(integer) The number of passes to roundtrip load/save each MSApp",
            getDefaultValue: () => 1
            );

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

        var validateCommand = new Command("validate", "Validate an group of existing MSApps at the specified path.")
        {
            folderPathOption,
            outPathOption,
            numPassesOption
        };
        validateCommand.SetHandler((folderpath, outpath, numpasses) =>
        {
            ValidateFunction(folderpath!.FullName, outpath!.FullName, numpasses);
        }, folderPathOption, outPathOption, numPassesOption);

        rootCommand.AddCommand(validateCommand);

        return rootCommand;
    }
}
