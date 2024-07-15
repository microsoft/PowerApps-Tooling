// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using System.Text.Json;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public class ValidatorFactory
{
    public Validator GetValidator()
    {
        // register schema in from memory into global schema registry
        var schemaLoader = new SchemaLoader();
        var serializedSchema = schemaLoader.Load();

        var evalOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        };

        // pass in serailization options for validator results object to json
        // This is unused for now but can be useful for producing raw json validation results which can be consumed elsewhere
        var resultSerializeOptions = new JsonSerializerOptions
        {
            Converters = { new EvaluationResultsJsonConverter() }
        };

        return new Validator(evalOptions, resultSerializeOptions, serializedSchema);
    }
}
