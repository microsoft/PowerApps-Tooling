// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Yaml2JsonNode;
using System.Text.Json;
using System.Text.Json.Nodes;
using Micorosoft.PowerPlatform.PowerApps.Persistence;
namespace Microsoft.PowerPlatform.PowerApps.Persistence;

public class Validator
{
    private readonly EvaluationOptions _verbosityOptions;
    private readonly JsonSerializerOptions _serializerOptions;


    public Validator(EvaluationOptions options, JsonSerializerOptions resultSerializeOptions)
    {
        // to do: add verbosity flag and allow users to choose verbosity of evaluation
        _verbosityOptions = options;
        _serializerOptions = resultSerializeOptions;

    }

    public YamlValidatorResults Validate(JsonSchema schema, string yamlFileData)
    {
        var yamlStream = YamlValidatorUtility.MakeYamlStream(yamlFileData);
        var jsonData = yamlStream.Documents.Count > 0 ? yamlStream.Documents[0].ToJsonNode() : JsonNode.Parse("{}");
        var results = schema.Evaluate(jsonData, _verbosityOptions);
        var output = JsonSerializer.Serialize(results, _serializerOptions);

        // TBD: remove, placeholder to view output for debugging
        Console.WriteLine(output);

        var schemaValidity = results.IsValid;
        // TBD: filter actual errors versus false positives
        // we look for errors that are not valid, have errors, and have an instance location (i.e are not oneOf errors)
        var yamlValidatorErrors = new List<YamlValidatorError>();
        if (!schemaValidity)
        {
            IReadOnlyList<EvaluationResults> traceList = results.Details.Where(
             node => !node.IsValid &&
             node.HasErrors).ToList();
            foreach (var trace in traceList)
            {
                yamlValidatorErrors.Add(new YamlValidatorError(trace));
            }
        }
        IReadOnlyList<YamlValidatorError> fileErrors = yamlValidatorErrors;
        var finalResults = new YamlValidatorResults(results.IsValid, fileErrors);
        return finalResults;

    }
}
