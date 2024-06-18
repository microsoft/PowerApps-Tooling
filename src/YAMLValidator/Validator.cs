// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Yaml2JsonNode;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal sealed class Validator
{
    private readonly EvaluationOptions _verbosityOptions;
    private readonly JsonSerializerOptions _serializerOptions;


    public Validator(EvaluationOptions options, JsonSerializerOptions resultSerializeOptions)
    {
        // to do: add verbosity flag and allow users to choose verbosity of evaluation
        _verbosityOptions = options;
        _serializerOptions = resultSerializeOptions;

    }

    public IReadOnlyList<YamlValidatorError> Validate(JsonSchema schema, string yamlFileData)
    {
        var yamlStream = YamlValidatorUtility.MakeYamlStream(yamlFileData);
        var jsonData = yamlStream.Documents.Count > 0 ? yamlStream.Documents[0].ToJsonNode() : JsonNode.Parse("{}");
        var results = schema.Evaluate(jsonData, _verbosityOptions);
        var output = JsonSerializer.Serialize(results, _serializerOptions);

        // filter actual errors versus false positives
        // we look for errors that are not valid, have errors, and have an instance location (i.e are not oneOf errors)
        IReadOnlyList<EvaluationResults> traceList = results.Details.Where(
            node => !node.IsValid &&
                    !string.IsNullOrEmpty(node.InstanceLocation.ToString()) &&
                     node.HasErrors).ToList();
        var yamlValidatorErrors = new List<YamlValidatorError>();
        foreach (var trace in traceList)
        {
            yamlValidatorErrors.Add(new YamlValidatorError(trace));

        }
        IReadOnlyList<YamlValidatorError> fileErrors = yamlValidatorErrors;
        return fileErrors;

    }
}
