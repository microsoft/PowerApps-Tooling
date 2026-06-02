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
            return new ValidatorResults(false, [new(Constants.notYamlError)]);
        }

        // here we say that empty yaml is serialized as null json
        if (yamlStream.Documents.Count <= 0)
        {
            return new(false, [new(Constants.emptyYamlError)]);
        }

        var jsonData = yamlStream.Documents[0].ToJsonNode();
        var results = _schema.Evaluate(jsonData, _verbosityOptions);

        var yamlValidatorErrors = new List<ValidatorError>();
        if (!results.IsValid && results.Details is not null)
        {
            var traceList = results.Details.Where(node => !node.IsValid);
            foreach (var trace in traceList)
            {
                var instanceLocation = trace.InstanceLocation.ToString();
                var schemaPath = trace.EvaluationPath.ToString();
                yamlValidatorErrors.Add(new(instanceLocation, schemaPath, trace.Errors));
            }
        }

        return new(results.IsValid, yamlValidatorErrors);
    }
}
