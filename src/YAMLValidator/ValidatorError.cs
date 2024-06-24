// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;
public class ValidatorError
{
    public string InstanceLocation { get; }
    public string SchemaPath { get; }
    public IReadOnlyDictionary<string, string>? Errors { get; }

    public ValidatorError(EvaluationResults results)
    {
        InstanceLocation = results.InstanceLocation.ToString();
        SchemaPath = results.EvaluationPath.ToString();
        Errors = results.Errors;
    }
    public ValidatorError(string error)
    {
        InstanceLocation = "";
        SchemaPath = "";
        Errors = new Dictionary<string, string> { { "", error } };
    }

    public override string ToString()
    {
        var errString = "";
        if (Errors != null)
        {
            foreach (var error in Errors)
            {
                var errType = string.IsNullOrEmpty(error.Key) ? "Error" : error.Key;
                errString += $"\t{errType}: {error.Value}\n";
            }
        }
        return $"InstanceLocation: {InstanceLocation}\nSchemaPath: {SchemaPath}\nErrors:\n{errString}";
    }
}
