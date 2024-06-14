// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class Orchestrator
{
    private readonly YamlLoader _fileLoader;
    private readonly SchemaLoader _schemaLoader;
    private readonly Validator _validator;

    public Orchestrator(string filePath, string schemaPath, string pathType)
    {
        _fileLoader = new YamlLoader(filePath, pathType);
        _schemaLoader = new SchemaLoader(schemaPath);
        _validator = new Validator();
    }

    public void runValidation()
    {
        _validator.Schema = _schemaLoader.Schema;
        foreach (var yamlFileData in _fileLoader.YamlData)
        {
            Console.WriteLine($"Validation for {yamlFileData.Key}");
            _validator.Yaml = yamlFileData.Value;
            var result = _validator.Validate();
            Console.WriteLine($"Is valid: {result}");
        }
    }



}
