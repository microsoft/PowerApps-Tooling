// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

internal class SchemaLoader
{
    private const string _schemaFolderPath = "schema";
    private const string _subschemaFolderPath = "subschemas";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Suppress to make classes stateless")]
    public JsonSchema Load()
    {
        var assembly = typeof(SchemaLoader).Assembly;
        var assemblyName = assembly.GetName().Name;

        JsonSchema? node = null;
        foreach (var file in assembly.GetManifestResourceNames())
        {
            string jsonSchemaString = ReadSchemaFromManifestFile(assembly, file);
            var schema = JsonSchema.FromText(jsonSchemaString);

            // assembly name is Microsoft.PowerPlatform.PowerApps.Persistence
            // subNamespace is YamlValidator, schemas live in the linked schema folder
            var rootFileName = $"{assemblyName}.{_schemaFolderPath}";

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
            throw new YamlValidatorLibraryException("The schema could not be serialized from the assembly.");
        }
        return node;

        static string ReadSchemaFromManifestFile(System.Reflection.Assembly assembly, string file)
        {
            var fileStream = assembly.GetManifestResourceStream(file)
                ?? throw new YamlValidatorLibraryException($"The schema could not be loaded from assembly.");
            using var streamReader = new StreamReader(fileStream);
            return streamReader.ReadToEnd();
        }
    }
}

