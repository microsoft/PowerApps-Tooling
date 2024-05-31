// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


//using System.CommandLine;
//using System.IO;

using System.CommandLine;

namespace YAMLValidator;
internal sealed class InputProcessor
{

    public static RootCommand GetRootCommand()
    {

        const string FileTypeName = "file";
        const string FolderTypeName = "folder";
        const string yamlFileExtension = ".yaml";

        // windows handles edge case where file and folder have same name
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
                if (Directory.GetFiles(inputFilePath, $"*{yamlFileExtension}").Length == 0)
                {
                    result.ErrorMessage = "The input folder does not contain any yaml files";
                }
            }
            else if (File.Exists(inputFilePath))
            {
                if (Path.GetExtension(inputFilePath) != yamlFileExtension)
                {
                    result.ErrorMessage = "The input file must be a yaml file";
                }
            }
        });

        var schemaOption = new Option<string>(
            name: "--schema",
            description: "The path to the schema yaml file",
            getDefaultValue: () => @"..\schemas\pa-yaml\v3.0\pa.schema.yaml"
        );

        schemaOption.AddValidator(result =>
        {
            var schemaPath = result.GetValueForOption(schemaOption);
            if (string.IsNullOrEmpty(schemaPath))
            {
                result.ErrorMessage = "Schema option selected, but no schema was provided";
            }
        });

        // define root
        var rootCommand = new RootCommand("YAML validator cli-tool");

        // commands
        // validate
        var validateCommand = new Command("validate", "Validate the input yaml file")
        {
            pathOption,
            schemaOption
        };

        validateCommand.SetHandler((pathOptionVal, schemaOptionVal) =>
        {
            // validation has completed, we either have a file or folder
            var fileType = File.Exists(pathOptionVal) ? FileTypeName : FolderTypeName;
            Console.WriteLine($@"Validating
                                 Path: {pathOptionVal}
                                 Filetype: {fileType}
                                 Schema: {schemaOptionVal}");
        }, pathOption, schemaOption);

        rootCommand.AddCommand(validateCommand);

        return rootCommand;

    }
}
