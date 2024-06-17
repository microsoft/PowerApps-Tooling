// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class Orchestrator
{
    private readonly YamlLoader _fileLoader;
    private readonly SchemaLoader _schemaLoader;
    private readonly Validator _validator;

    public Orchestrator(YamlLoader fileLoader, SchemaLoader schemaLoader, Validator validator)
    {
        _fileLoader = fileLoader;
        _schemaLoader = schemaLoader;
        _validator = validator;
    }

    public void RunValidation(FilePathData inputData)
    {
        var schemaPath = inputData.SchemaPath;
        var path = inputData.FilePath;
        var pathType = inputData.FilePathType;

        var yamlData = _fileLoader.Load(path, pathType);
        var serializedSchema = _schemaLoader.Load(schemaPath);


        foreach (var yamlFileData in yamlData)
        {
            Console.WriteLine($"Validation for {yamlFileData.Key}");
            var result = _validator.Validate(serializedSchema, yamlFileData.Value);
            Console.WriteLine($"Is valid: {result}");
        }
    }



}
