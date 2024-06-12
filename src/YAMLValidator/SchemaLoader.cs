// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class SchemaLoader
{
    public JsonSchema _schema { get; }
    private const string _schemaFolderPath = "subschemas";
    private const string _jsonFileExtension = ".json";
    public SchemaLoader(string schemaPath)
    {
        _schema = LoadSchema(schemaPath);
    }

    private static JsonSchema LoadSchema(string schemaPath)
    {
        // error handled by System.commandLine validator
        var node = JsonSchema.FromFile(schemaPath);
        var schemaFolder = Path.GetDirectoryName(schemaPath);
        var subschemaPaths = Directory.GetFiles($@"{schemaFolder}\{_schemaFolderPath}", $"*{_jsonFileExtension}");
        foreach (var path in subschemaPaths)
        {
            var subschema = JsonSchema.FromFile(path);
            SchemaRegistry.Global.Register(subschema);
        }

        return node;
    }

}

