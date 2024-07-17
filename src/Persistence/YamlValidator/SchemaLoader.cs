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

            // assembly name is Microsoft.PowerPlatform.PowerApps.Persistence
            // subNamespace is YamlValidator, schemas live in the linked schema folder
            var rootFileName = $"{assemblyName}.{Constants.subNamespace}.{_schemaFolderPath}";

            if (file.StartsWith($"{rootFileName}.{_subschemaFolderPath}.", StringComparison.Ordinal))
            {
                // these virtual uri's are used to resolve $ref's in the schema, they aren't
                // represented like this in the dll
                schema.BaseUri = new Uri($"file://{_schemaFolderPath}/{_subschemaFolderPath}/");
                SchemaRegistry.Global.Register(schema);
                continue;
            }
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

