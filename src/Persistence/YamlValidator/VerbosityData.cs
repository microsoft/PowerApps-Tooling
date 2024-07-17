// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json;
using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.YamlValidator;
public readonly record struct VerbosityData
{
    public EvaluationOptions EvalOptions { get; }
    public JsonSerializerOptions JsonOutputOptions { get; }

    public VerbosityData(string verbosityLevel)
    {
        EvalOptions = new EvaluationOptions();
        JsonOutputOptions = new JsonSerializerOptions { Converters = { new EvaluationResultsJsonConverter() } };

        if (verbosityLevel == Constants.Verbose)
        {
            EvalOptions.OutputFormat = OutputFormat.List;
            return;
        }
        EvalOptions.OutputFormat = OutputFormat.Flag;
    }
}

