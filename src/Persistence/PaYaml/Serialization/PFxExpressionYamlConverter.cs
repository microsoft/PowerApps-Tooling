// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

// BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
public class PFxExpressionYamlConverter : IYamlTypeConverter
{
    private readonly PFxExpressionYamlFormattingOptions _formattingOptions;

    public PFxExpressionYamlConverter(PFxExpressionYamlFormattingOptions formattingOptions)
    {
        _formattingOptions = formattingOptions ?? throw new ArgumentNullException(nameof(formattingOptions));
    }

    public bool Accepts(Type type)
    {
        return type == typeof(PFxExpressionYaml);
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        if (parser.TryConsumeNull())
            return null;

        var scalar = parser.Consume<Scalar>();

        // Detect canonical format, where expressions should always start with '=' character
        if (scalar.Value.Length >= 1 && scalar.Value[0] == PFxExpressionYamlFormattingOptions.ScalarPrefix)
        {
            var script = scalar.Value[1..];
            return new PFxExpressionYaml(script);
        }

        throw new PaYamlSerializationException($"Power Fx expressions must start with '{PFxExpressionYamlFormattingOptions.ScalarPrefix}'.", scalar.Start);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var expression = (PFxExpressionYaml?)value;
        if (expression is null)
        {
            emitter.EmitNull();
            return;
        }

        // If the script contains a substring that would cause extra yaml escaping, force it to a literal block
        // The goal here for PFx yaml is that we avoid using yaml string escaping whenever possible.
        var forceLiteralBlock =
            // These char sequences are special to YAML parsers, so must be escaped or forced to literal block
            expression.InvariantScript.Contains(" #")
            || expression.InvariantScript.Contains(": ")
            // Force multi-line scripts to be literal blocks
            || expression.InvariantScript.Contains('\n')
            || expression.InvariantScript.Contains('\r');
        if (!forceLiteralBlock && _formattingOptions.ForceLiteralBlockIfContainsAny?.Count > 0)
        {
            // e.g. our original code was forcing literal block if the script contained any double quotes (`"`).
            forceLiteralBlock |= _formattingOptions.ForceLiteralBlockIfContainsAny.Any(expression.InvariantScript.Contains);
        }

        // Construct the scalar value to emit, which must have the '=' prefix
        var yamlScalarValue = $"{PFxExpressionYamlFormattingOptions.ScalarPrefix}{expression.InvariantScript}";

        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, yamlScalarValue, forceLiteralBlock ? ScalarStyle.Literal : ScalarStyle.Any,
            isPlainImplicit: true, isQuotedImplicit: !forceLiteralBlock));
    }
}
