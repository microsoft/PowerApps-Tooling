// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using YAMLValidator;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class InputProcessor
{

    private static void ProcessFiles(string path, string schema, string pathType)
    {
        // Orchestrator class to handle the processing of the input files
        var filePathInfo = new FilePathData(path, schema, pathType);
        var orchestrator = new Orchestrator(filePathInfo.FilePath,
            filePathInfo.SchemaPath, filePathInfo.FilePathType);
        orchestrator.runValidation();
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
            var pathType = string.Empty;
            if (string.IsNullOrEmpty(inputFilePath))
            {
                result.ErrorMessage = "The input is invalid, input must be a filepath to a yaml file \\" +
                "or a folder path to a folder of yaml files";
            }
            else if (!Directory.Exists(inputFilePath) && !File.Exists(inputFilePath))
            {
                result.ErrorMessage = "The input path does not exist";
            }
            else if (Directory.Exists(inputFilePath))
            {
                if (Directory.GetFiles(inputFilePath, $"*{YamlValidatorConstants.YamlFileExtension}").Length == 0)
                {
                    result.ErrorMessage = "The input folder does not contain any yaml files";
                }
            }
            else if (File.Exists(inputFilePath))
            {
                if (Path.GetExtension(inputFilePath) != YamlValidatorConstants.YamlFileExtension)
                {
                    result.ErrorMessage = "The input file must be a yaml file";
                }
            }
        });

        // assume local schema file exists in nuget package, use relative filepath for now
        var schemaOption = new Option<string>(
            name: "--schema",
            description: "The path to the schema json file",
            getDefaultValue: () => @".\schema\pa.yaml-schema.json"
            );

        schemaOption.AddValidator(result =>
        {
            var schemaPath = result.GetValueForOption(schemaOption);
            if (string.IsNullOrEmpty(schemaPath))
            {
                result.ErrorMessage = "Schema option selected, but no schema was provided";
            }
            else if (Path.GetExtension(schemaPath) != YamlValidatorConstants.JsonFileExtension)
            {
                result.ErrorMessage = "The schema file must be a json file";
            }
            else if (!File.Exists(schemaPath))
            {
                result.ErrorMessage = "The schema file does not exist";
            }
        });

        // define root
        var rootCommand = new RootCommand("YAML validator cli-tool");

        // validate command
        var validateCommand = new Command("validate", "Validate the input yaml file")
        {
            pathOption,
            schemaOption
        };

        validateCommand.SetHandler((pathOptionVal, schemaOptionVal) =>
        {
            // validation has completed, we either have a file or folder
            var pathType = File.Exists(pathOptionVal) ? YamlValidatorConstants.FileTypeName :
                                                        YamlValidatorConstants.FolderTypeName;
            ProcessFiles(pathOptionVal, schemaOptionVal, pathType);

        }, pathOption, schemaOption);

        rootCommand.AddCommand(validateCommand);

        return rootCommand;

    }
}
