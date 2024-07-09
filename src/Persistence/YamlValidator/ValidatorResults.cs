// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;
public class ValidatorResults
{
    public bool SchemaValid { get; }
    public IReadOnlyList<ValidatorError> TraversalResults { get; }

    public ValidatorResults(bool schemaValid, IReadOnlyList<ValidatorError> traversalResults)
    {
        SchemaValid = schemaValid;
        TraversalResults = FilterErrors(traversalResults);
    }

    //  This will filter out the false positives that are not relevant to the error output, when the validation is false
    private ReadOnlyCollection<ValidatorError> FilterErrors(IReadOnlyList<ValidatorError> traversalResults)
    {
        var maxSchemaArraySuffixSize = 0;
        var maxSchemaObjectSuffixSize = 0;
        var arrayTypeSchemaPath = "/oneOf/1";
        var objectTypeSchemaPath = "/oneOf/0";
        foreach (var err in traversalResults)
        {
            var errSchemaPath = err.SchemaPath;
            if (!errSchemaPath.StartsWith(arrayTypeSchemaPath, StringComparison.Ordinal) &&
                !err.SchemaPath.StartsWith(objectTypeSchemaPath, StringComparison.Ordinal))
            {
                continue;
            }

            var suffixLength = errSchemaPath.Length - arrayTypeSchemaPath.Length;
            if (errSchemaPath.StartsWith(arrayTypeSchemaPath, StringComparison.Ordinal))
            {
                maxSchemaArraySuffixSize = Math.Max(maxSchemaArraySuffixSize, suffixLength);
            }
            else
            {
                maxSchemaObjectSuffixSize = Math.Max(maxSchemaObjectSuffixSize, suffixLength);
            }
        }
        var filteredErrors = new List<ValidatorError>();
        foreach (var err in traversalResults)
        {
            var errSchemaPath = err.SchemaPath;
            if (!errSchemaPath.StartsWith(arrayTypeSchemaPath, StringComparison.Ordinal) &&
                !err.SchemaPath.StartsWith(objectTypeSchemaPath, StringComparison.Ordinal))
            {
                filteredErrors.Add(err);
                continue;
            }

            if (errSchemaPath.StartsWith(arrayTypeSchemaPath, StringComparison.Ordinal))
            {
                if (maxSchemaArraySuffixSize >= maxSchemaObjectSuffixSize)
                {
                    filteredErrors.Add(err);
                }
            }
            else
            {
                if (maxSchemaObjectSuffixSize >= maxSchemaArraySuffixSize)
                {
                    filteredErrors.Add(err);
                }
            }

        }

        return filteredErrors.AsReadOnly();
    }
}
