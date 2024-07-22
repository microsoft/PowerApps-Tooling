// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

public class ValidatorError
{
    public string InstanceLocation { get; }
    public string SchemaPath { get; }
    public IReadOnlyDictionary<string, string>? Errors { get; }

    public ValidatorError(string instancePath, string schemaPath, IReadOnlyDictionary<string, string>? errors)
    {
        InstanceLocation = instancePath;
        SchemaPath = schemaPath;
        Errors = errors;
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
