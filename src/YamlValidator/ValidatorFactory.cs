// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

internal class ValidatorFactory : IValidatorFactory
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "required by IValidatorFactory interface")]
    public IValidator CreateValidator()
    {
        // register schema in from memory into global schema registry
        var schemaLoader = new SchemaLoader();
        var serializedSchema = schemaLoader.Load();

        var evalOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List
        };

        return new Validator(evalOptions, serializedSchema);
    }
}
