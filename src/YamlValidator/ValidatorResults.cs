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
        TraversalResults = filterErrors(traversalResults);
    }

    /* *
     * Fix: this will filter out the false positives that are not relevant to the error output, when the validation is false
     * Filters the errors to only include the errors that are relevant to the schema.
     * For all instance paths in the schema with paths oneOf/0 and oneOf/1, we find the max suffix size and only keep the errors 
     * from that OneOf path with the max suffix size.
     * Why? since errors are propogated up the schema validation tree, the errors at the leaf nodes are the 
     * most relevant but can have false positives due to the OneOf statement in the schema.
     * */
    private ReadOnlyCollection<ValidatorError> filterErrors(IReadOnlyList<ValidatorError> traversalResults)
    {
        var maxSchemaArraySuffixSize = 0;
        var maxSchemaObjectSuffixSize = 0;
        var arrayTypeFile = "/oneOf/1";
        var objectTypeFile = "/oneOf/0";
        foreach (var err in traversalResults)
        {
            var errSchemaPath = err.SchemaPath;
            if (!errSchemaPath.StartsWith(arrayTypeFile, StringComparison.Ordinal) &&
                !err.SchemaPath.StartsWith(objectTypeFile, StringComparison.Ordinal))
            {
                continue;
            }

            var suffixLength = errSchemaPath.Length - arrayTypeFile.Length;
            if (errSchemaPath.StartsWith(arrayTypeFile, StringComparison.Ordinal))
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
            if (!errSchemaPath.StartsWith(arrayTypeFile, StringComparison.Ordinal) &&
                !err.SchemaPath.StartsWith(objectTypeFile, StringComparison.Ordinal))
            {
                filteredErrors.Add(err);
                continue;
            }

            if (errSchemaPath.StartsWith(arrayTypeFile, StringComparison.Ordinal))
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
