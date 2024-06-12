// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.Text.Json;
using Json.Schema;
using Yaml2JsonNode;
namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal sealed class Validator
{
    public JsonSchema? _schema { get; set; }
    public string? _yaml { get; set; }

    private readonly EvaluationOptions _verbosityOptions;
    private readonly JsonSerializerOptions _serializerOptions;


    public Validator()
    {
        // to do: add verbosity flag and allow users to choose verbosity of evaluation
        _verbosityOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Flag
        };
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new EvaluationResultsJsonConverter() }
        };
    }

    public string Validate()
    {
        if (_schema == null || _yaml == null)
        {
            throw new InvalidOperationException("Schema or Yaml is not set");
        }

        try
        {
            var yamlStream = YamlValidatorUtility.MakeYamlStream(_yaml);
            var jsonData = yamlStream.Documents[0].ToJsonNode();
            var results = _schema.Evaluate(jsonData, _verbosityOptions);
            var output = JsonSerializer.Serialize(results, _serializerOptions);
            return output;
        }
        catch (IndexOutOfRangeException e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
        catch (RefResolutionException e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
}
