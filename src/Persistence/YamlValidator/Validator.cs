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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private member",
    Justification = "Suppress serializing the raw validator errors into json will be useful for future use")]
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly JsonSchema _schema;
    public Validator(EvaluationOptions options, JsonSerializerOptions resultSerializeOptions, JsonSchema schema)
    {
        // to do: add verbosity flag and allow users to choose verbosity of evaluation
        _verbosityOptions = options;
        _serializerOptions = resultSerializeOptions;
        _schema = schema;
    }
    public ValidatorResults Validate(string yamlFileData)
    {
        YamlStream yamlStream;
        try
        {
            yamlStream = Utility.MakeYamlStream(yamlFileData);
        }
        catch (YamlException)
        {
            return new ValidatorResults(false, new List<ValidatorError> { new(Constants.notYamlError) });
        }

        var jsonData = yamlStream.Documents.Count > 0 ? yamlStream.Documents[0].ToJsonNode() : null;

        // here we say that empty yaml is serialized as null json
        if (jsonData == null)
        {
            return new ValidatorResults(false, new List<ValidatorError> { new(Constants.emptyYamlError) });
        }
        var results = _schema.Evaluate(jsonData, _verbosityOptions);

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
