// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class InputProcessor
{
    public static RootCommand GetRootCommand()
    {

        const string FileTypeName = "file";
        const string FolderTypeName = "folder";
        const string YamlFileExtension = ".yaml";
        const string JsonFileExtension = ".json";

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
                result.ErrorMessage = "The input is invalid, input must be a filepath to a yaml file or a folder path to a folder of yaml files";
            }
            else if (!Directory.Exists(inputFilePath) && !File.Exists(inputFilePath))
            {
                result.ErrorMessage = "The input path does not exist";
            }
            else if (Directory.Exists(inputFilePath))
            {
                if (Directory.GetFiles(inputFilePath, $"*{YamlFileExtension}").Length == 0)
                {
                    result.ErrorMessage = "The input folder does not contain any yaml files";
                }
            }
            else if (File.Exists(inputFilePath))
            {
                if (Path.GetExtension(inputFilePath) != YamlFileExtension)
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
            else if (Path.GetExtension(schemaPath) != JsonFileExtension)
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
            var fileType = File.Exists(pathOptionVal) ? FileTypeName : FolderTypeName;
            Console.WriteLine($"ValidatingPath: {pathOptionVal}");
            Console.WriteLine($"Path type: {fileType}");
            Console.WriteLine($"Schema: {schemaOptionVal}");

            // to do -> add handler to validate all yaml files in a folder are actually parseable as yaml
            //         or add handler to validate a single yaml file is parseable as yaml
        }, pathOption, schemaOption);

        rootCommand.AddCommand(validateCommand);

        return rootCommand;

    }
}
