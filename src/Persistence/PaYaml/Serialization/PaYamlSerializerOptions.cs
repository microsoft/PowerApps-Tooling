// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3_0;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public record PaYamlSerializerOptions
{
    public static readonly PaYamlSerializerOptions Default = new();

    public string NewLine { get; init; } = "\n";

    public PFxExpressionYamlFormattingOptions PFxExpressionYamlFormatting { get; init; } = new();

    public Action<DeserializerBuilder>? AdditionalDeserializerConfiguration { get; init; }

    public Action<SerializerBuilder>? AdditionalSerializerConfiguration { get; init; }

    internal void ApplyToDeserializerBuilder(DeserializerBuilder builder, PaSerializationContext serializationContext)
    {
        builder
            .WithDuplicateKeyChecking()
            ;
        AddTypeConverters(builder, serializationContext);
        AdditionalDeserializerConfiguration?.Invoke(builder);
    }

    internal void ApplyToSerializerBuilder(SerializerBuilder builder, PaSerializationContext serializationContext)
    {
        ApplySerializerFormatting(builder);
        AddTypeConverters(builder, serializationContext);
        AdditionalSerializerConfiguration?.Invoke(builder);
    }

    private void ApplySerializerFormatting(SerializerBuilder builder)
    {
        // TODO: Can we control indentation chars? e.g. to be explicitly set to 2 spaces?
        builder
            .WithQuotingNecessaryStrings()
            .WithNewLine(NewLine)
            .DisableAliases()
            .WithEnumNamingConvention(PascalCaseNamingConvention.Instance)
            .WithIndentedSequences() // to match VS Code's default formatting settings
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)
            ;
    }

    private void AddTypeConverters<TBuilder>(BuilderSkeleton<TBuilder> builder, PaSerializationContext serializationContext)
        where TBuilder : BuilderSkeleton<TBuilder>
    {
        builder.WithTypeConverter(new PFxExpressionYamlConverter(PFxExpressionYamlFormatting));
        builder.WithTypeConverter(new NamedObjectYamlConverter<ControlInstance>(serializationContext));
        builder.WithTypeConverter(new NamedObjectYamlConverter<PFxFunctionParameter>(serializationContext));
    }

    // BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
    /// <summary>
    /// Applies the formatting options on this instance to a serializer.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "TESTONLY")]
    public void TESTONLY_ApplySerializerFormatting(SerializerBuilder builder)
    {
        ApplySerializerFormatting(builder);
    }
}
