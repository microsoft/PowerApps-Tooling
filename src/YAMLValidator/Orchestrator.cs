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
        _validator._schema = _schemaLoader._schema;
        foreach (var yamlFileData in _fileLoader._yamlData)
        {
            Console.WriteLine($"Validation for {yamlFileData.Key}");
            _validator._yaml = yamlFileData.Value;
            var result = _validator.Validate();
            Console.WriteLine(result);
        }
    }



}
