// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml;

public record PaYamlSerializerOptions
{
    internal static readonly PaYamlSerializerOptions Default = new();

    public string NewLine { get; init; } = "\n";
    public bool IsTextFirst { get; init; }
    public PFxExpressionYamlFormattingOptions PFxExpressionYamlFormatting { get; init; } = new();

    public Action<DeserializerBuilder>? AdditionalDeserializerConfiguration { get; init; }

    public Action<SerializerBuilder>? AdditionalSerializerConfiguration { get; init; }

    internal void ApplyToDeserializerBuilder(DeserializerBuilder builder)
    {
        builder
            .WithDuplicateKeyChecking()
            ;
        AddTypeConverters(builder);
        AdditionalDeserializerConfiguration?.Invoke(builder);
    }

    internal void ApplyToSerializerBuilder(SerializerBuilder builder)
    {
        builder
            .WithQuotingNecessaryStrings()
            .WithNewLine(NewLine)
            .DisableAliases()
            //.WithDefaultScalarStyle(ScalarStyle.Any) // default is Any
            .WithEnumNamingConvention(PascalCaseNamingConvention.Instance)
            // TODO: Can we control indentation chars? e.g. to be explicitly set to 2 spaces?
            .WithIndentedSequences() // to match VS Code's default formatting settings
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)
            ;
        AddTypeConverters(builder);
        AdditionalSerializerConfiguration?.Invoke(builder);
    }

    private void AddTypeConverters<TBuilder>(BuilderSkeleton<TBuilder> builder)
        where TBuilder : BuilderSkeleton<TBuilder>
    {
        builder.WithTypeConverter(new PFxExpressionYamlConverter(PFxExpressionYamlFormatting));

        //.WithObjectFactory(new ControlObjectFactory(_controlTemplateStore, _controlFactory))
        //.WithTypeInspector(inner => new ControlTypeInspector(inner, _controlTemplateStore))
        //.WithTypeDiscriminatingNodeDeserializer(o =>
        //{
        //    o.AddTypeDiscriminator(new ControlTypeDiscriminator(_controlTemplateStore));
        //})
    }
}
