// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal sealed class SchemaLoader
{
    private const string _schemaFolderPath = "subschemas";

    public JsonSchema Load(string schemaPath)
    {
        var node = JsonSchema.FromFile(schemaPath);
        var schemaFolder = Path.GetDirectoryName(schemaPath);
        var subschemaPaths = Directory.GetFiles($@"{schemaFolder}\{_schemaFolderPath}",
            $"*{YamlValidatorConstants.JsonFileExtension}");

        foreach (var path in subschemaPaths)
        {
            var subschema = JsonSchema.FromFile(path);
            SchemaRegistry.Global.Register(subschema);
        }

        return node;
    }

}

