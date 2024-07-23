// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Yaml2JsonNode;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;

internal class Validator : IValidator
{
    private readonly EvaluationOptions _verbosityOptions;
    private readonly JsonSchema _schema;
    public Validator(EvaluationOptions options, JsonSchema schema)
    {
        // to do: add verbosity flag and allow users to choose verbosity of evaluation
        _verbosityOptions = options;
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
                var instanceLocation = trace.InstanceLocation.ToString();
                var schemaPath = trace.EvaluationPath.ToString();
                var errors = trace.Errors;
                yamlValidatorErrors.Add(new ValidatorError(instanceLocation, schemaPath, errors));
            }
        }

        IReadOnlyList<ValidatorError> fileErrors = yamlValidatorErrors;
        var finalResults = new ValidatorResults(results.IsValid, fileErrors);
        return finalResults;
    }
}
