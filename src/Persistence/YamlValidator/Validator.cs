// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Yaml2JsonNode;
using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

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

    public ValidatorResults Validate(JsonSchema schema, string yamlFileData)
    {
        YamlStream yamlStream;
        try
        {
            yamlStream = Utility.MakeYamlStream(yamlFileData);
        }
        catch (YamlException)
        {
            return new ValidatorResults(false, new List<ValidatorError> { new("File is not yaml") });
        }

        var jsonData = yamlStream.Documents.Count > 0 ? yamlStream.Documents[0].ToJsonNode() : null;

        // here we say that empty yaml is serialized as null json
        if (jsonData == null)
        {
            return new ValidatorResults(false, new List<ValidatorError> { new("Empty YAML file") });
        }
        var results = schema.Evaluate(jsonData, _verbosityOptions);

        // not used but may help if we ever need to serialize the evaluation results into json format to feed into
        // a VSCode extension or other tool
        var output = JsonSerializer.Serialize(results, _serializerOptions);

        var schemaValidity = results.IsValid;
        var yamlValidatorErrors = new List<ValidatorError>();
        if (!schemaValidity)
        {
            IReadOnlyList<EvaluationResults> traceList = results.Details.Where(
             node => !node.IsValid &&
             node.HasErrors).ToList();
            foreach (var trace in traceList)
            {
                yamlValidatorErrors.Add(new ValidatorError(trace));
            }
        }

        IReadOnlyList<ValidatorError> fileErrors = yamlValidatorErrors;
        var finalResults = new ValidatorResults(results.IsValid, fileErrors);
        return finalResults;
    }
}
