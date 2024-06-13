// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Yaml2JsonNode;
using System.Text.Json;
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
            OutputFormat = OutputFormat.Hierarchical
        };
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new EvaluationResultsJsonConverter() }
        };
    }

    public bool Validate()
    {
        if (_schema == null || _yaml == null)
        {
            Console.WriteLine("Schema or Yaml is not set");
            throw new InvalidOperationException();
        }

        try
        {
            var yamlStream = YamlValidatorUtility.MakeYamlStream(_yaml);
            if (yamlStream.Documents.Count == 0)
            {
                Console.WriteLine("The given file is empty");
                return true;
            }
            var jsonData = yamlStream.Documents[0].ToJsonNode();
            var results = _schema.Evaluate(jsonData, _verbosityOptions);
            var output = JsonSerializer.Serialize(results, _serializerOptions);
            Console.WriteLine(output);
            return results.IsValid;
        }
        catch (ArgumentOutOfRangeException e)
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
