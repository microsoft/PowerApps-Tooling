// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public class InputProcessor
{
    private static void ProcessFiles(string path, string schema, string pathType)
    {
        // read only records
        var filePathInfo = new ValidationRequest(path, schema, pathType);
        var verbosityInfo = new VerbosityData(Constants.Verbose);

        var validator = new Validator(verbosityInfo.EvalOptions, verbosityInfo.JsonOutputOptions);
        var schemaLoader = new SchemaLoader();
        var fileLoader = new YamlLoader();
        var orchestrator = new Orchestrator(fileLoader, schemaLoader, validator);
        orchestrator.RunValidation(filePathInfo);
    }

    public static RootCommand GetRootCommand()
    {

        var pathOption = new Option<string>(
            name: "--path",
            description: "The path to the input yaml file or directory of yaml files"
        )
        { IsRequired = true };

        pathOption.AddValidator(result =>
        {
            var inputFilePath = result.GetValueForOption(pathOption);

            // either file or folder must be passed
            if (string.IsNullOrWhiteSpace(inputFilePath))
            {
                result.ErrorMessage = "The input is invalid, input must be a filepath to a yaml file \\" +
                "or a folder path to a folder of yaml files";
            }
            else if (!Directory.Exists(inputFilePath) && !File.Exists(inputFilePath))
            {
                result.ErrorMessage = $"The path '{inputFilePath}' does not exist";
            }
            /*else if (Directory.Exists(inputFilePath))
            {
                if (Directory.GetFiles(inputFilePath, $"*{Constants.YamlFileExtension}").Length == 0)
                {
                    result.ErrorMessage = $"The folder '{inputFilePath}' does not contain any yaml files";
                }
            }*/
            else if (File.Exists(inputFilePath))
            {
                if (!inputFilePath.EndsWith(Constants.YamlFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = $"The file '{inputFilePath}' must be a '{Constants.YamlFileExtension}' file";
                }
            }
        });

        // assume local schema file exists in NuGet package, use relative filepath for now
        var schemaOption = new Option<string>(
            name: "--schema",
            description: "The path to the schema json file",
            getDefaultValue: () => Constants.DefaultSchemaPath
            );

        schemaOption.AddValidator(result =>
        {
            var schemaPath = result.GetValueForOption(schemaOption);
            if (string.IsNullOrEmpty(schemaPath))
            {
                result.ErrorMessage = "Schema option selected, but no schema was provided";
            }
            else if (Path.GetExtension(schemaPath) != Constants.JsonFileExtension)
            {
                result.ErrorMessage = "The schema file must be a json file";
            }
            /*else if (!File.Exists(schemaPath))
            {
                result.ErrorMessage = $"The schema file '{schemaPath}' does not exist";
            }*/
        });

        // define root
        var rootCommand = new RootCommand("Power Apps YAML validator command line tool");

        // validate command
        var validateCommand = new Command("validate", "Validate the input Power Apps YAML file")
        {
            pathOption,
            schemaOption
        };

        validateCommand.SetHandler((pathOptionVal, schemaOptionVal) =>
        {
            var pathType = File.GetAttributes(pathOptionVal).HasFlag(FileAttributes.Directory) ? Constants.FolderTypeName :
                                                                                                 Constants.FileTypeName;
            ProcessFiles(pathOptionVal, schemaOptionVal, pathType);

        }, pathOption, schemaOption);

        rootCommand.AddCommand(validateCommand);

        return rootCommand;
    }
}
