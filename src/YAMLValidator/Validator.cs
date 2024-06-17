// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using Yaml2JsonNode;
// using System.Text.Json;
using System.Text.Json.Nodes;
namespace Microsoft.PowerPlatform.PowerApps.Persistence;

internal sealed class Validator
{
    public JsonSchema? Schema { get; set; }
    public string? Yaml { get; set; }

    private readonly EvaluationOptions _verbosityOptions;
    // private readonly JsonSerializerOptions _serializerOptions;


    public Validator()
    {
        // to do: add verbosity flag and allow users to choose verbosity of evaluation
        _verbosityOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Hierarchical
        };
        // _serializerOptions = new JsonSerializerOptions
        // {
        //     Converters = { new EvaluationResultsJsonConverter() }
        // };
    }

    public bool Validate()
    {
        if (Schema == null || Yaml == null)
        {
            Console.WriteLine("Schema or Yaml is not set");
            throw new InvalidOperationException();
        }

        try
        {
            var yamlStream = YamlValidatorUtility.MakeYamlStream(Yaml);

            // handle empty yaml?
            var jsonData = yamlStream.Documents.Count > 0 ? yamlStream.Documents[0].ToJsonNode() :
                JsonNode.Parse("{}");

            var results = Schema.Evaluate(jsonData, _verbosityOptions);
            // var output = JsonSerializer.Serialize(results, _serializerOptions);
            // Console.WriteLine(output);
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
