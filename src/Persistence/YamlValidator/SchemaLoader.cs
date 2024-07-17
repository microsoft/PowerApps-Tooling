using System.Reflection;
using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public class SchemaLoader
{
    private const string _schemaFolderPath = "schema";
    private const string _subschemaFolderPath = "subschemas";

    public JsonSchema Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        JsonSchema? node = null;
        foreach (var file in assembly.GetManifestResourceNames())
        {
            var fileStream = assembly.GetManifestResourceStream(file);
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            if (fileStream == null)
            {
                throw new IOException($"Resource {file} could not found in assembly {assemblyName}");
            }
            using var streamReader = new StreamReader(fileStream);
            var jsonSchemaString = streamReader.ReadToEnd();
            var schema = JsonSchema.FromText(jsonSchemaString);
            if (file.StartsWith($"{assemblyName}.{_subschemaFolderPath}", StringComparison.Ordinal))
            {
                var subschemaNameLength = $"{assemblyName}.{_subschemaFolderPath}.".Length;
                var subschemaName = file.Substring(subschemaNameLength);
                schema.BaseUri = new Uri($"file://{_schemaFolderPath}/{_subschemaFolderPath}/");
                SchemaRegistry.Global.Register(schema);
                continue;

            }
            var schemaNameLength = $"{assemblyName}.".Length;
            var schemaName = file.Substring(schemaNameLength);
            schema.BaseUri = new Uri($"file://{_schemaFolderPath}");
            node = schema;
        }
        if (node == null)
        {
            throw new InvalidDataException("Schema was not able to be read into memory");
        }
        return node;
    }
}

