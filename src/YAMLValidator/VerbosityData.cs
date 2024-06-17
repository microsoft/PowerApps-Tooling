// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json;
using Json.Schema;

namespace Microsoft.PowerPlatform.PowerApps.Persistence;
internal readonly record struct VerbosityData
{
    public EvaluationOptions EvalOptions { get; }
    public JsonSerializerOptions JsonOutputOptions { get; }

    public VerbosityData(string verbosityLevel)
    {
        EvalOptions = new EvaluationOptions();
        JsonOutputOptions = new JsonSerializerOptions { Converters = { new EvaluationResultsJsonConverter() } };

        if (verbosityLevel == YamlValidatorConstants.verbose)
        {
            EvalOptions.OutputFormat = OutputFormat.Hierarchical;
            return;
        }
        EvalOptions.OutputFormat = OutputFormat.Flag;
    }
}

