// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using Microsoft.PowerPlatform.PowerApps.Persistence;

namespace Micorosoft.PowerPlatform.PowerApps.Persistence;
internal class YamlValidatorResults
{
    public bool SchemaValid { get; }
    public IReadOnlyList<YamlValidatorError> TraversalResults { get; }

    public YamlValidatorResults(bool schemaValid, IReadOnlyList<YamlValidatorError> traversalResults)
    {
        SchemaValid = schemaValid;
        TraversalResults = traversalResults;

    }
}
