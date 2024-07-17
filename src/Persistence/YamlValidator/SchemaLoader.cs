// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public class SchemaLoader
{
    private const string _schemaFolderPath = "subschemas";
    private static readonly string _schemaPath = Path.Combine(".", "YamlValidator", "schema", "pa.yaml-schema.json");

    public JsonSchema Load()
    {
        var node = JsonSchema.FromFile(_schemaPath);
        var schemaFolder = Path.GetDirectoryName(_schemaPath);
        var subschemaPaths = Directory.GetFiles($@"{schemaFolder}{Path.DirectorySeparatorChar}{_schemaFolderPath}",
            $"*{Constants.JsonFileExtension}");

        foreach (var path in subschemaPaths)
        {
            var subschema = JsonSchema.FromFile(path);
            SchemaRegistry.Global.Register(subschema);
        }

        return node;
    }
}

