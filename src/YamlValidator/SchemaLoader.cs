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

        JsonSchema? node = null;
        foreach (var file in assembly.GetManifestResourceNames())
        {
            var fileStream = assembly.GetManifestResourceStream(file);
            var assemblyName = assembly.GetName().Name;
            if (fileStream == null)
            {
                throw new YamlValidatorLibraryException($"The schema could not be loaded from assembly.");
            }
            using var streamReader = new StreamReader(fileStream);
            var jsonSchemaString = streamReader.ReadToEnd();
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
    }
}

