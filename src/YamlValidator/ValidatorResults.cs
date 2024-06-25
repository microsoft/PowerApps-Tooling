// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;
public class ValidatorResults
{
    public bool SchemaValid { get; }
    public IReadOnlyList<ValidatorError> TraversalResults { get; }

    public ValidatorResults(bool schemaValid, IReadOnlyList<ValidatorError> traversalResults)
    {
        SchemaValid = schemaValid;
        TraversalResults = traversalResults;
    }
}
