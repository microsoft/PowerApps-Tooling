// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public class PaYamlSerializationContext : IDisposable
{
    private bool _isDisposed;

    public PaYamlSerializationContext(PaYamlSerializerOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// The options used when creating this context.
    /// </summary>
    public PaYamlSerializerOptions Options { get; }

    internal void ApplyToDeserializerBuilder(DeserializerBuilder builder)
    {
        builder
            .WithDuplicateKeyChecking()
            .IgnoreFields()
            ;

        AddTypeConverters(builder);
        Options.AdditionalDeserializerConfiguration?.Invoke(builder);
    }

    internal void ApplyToSerializerBuilder(SerializerBuilder builder)
    {
        ApplySerializerFormatting(builder);
        AddTypeConverters(builder);
        Options.AdditionalSerializerConfiguration?.Invoke(builder);
    }

    private void ApplySerializerFormatting(SerializerBuilder builder)
    {
        // TODO: Can we control indentation chars? e.g. to be explicitly set to 2 spaces?
        builder
            .WithQuotingNecessaryStrings()
            .WithNewLine(Options.NewLine)
            .DisableAliases()
            .WithEnumNamingConvention(PascalCaseNamingConvention.Instance)
            .WithIndentedSequences() // to match VS Code's default formatting settings
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)
            ;
    }

    private void AddTypeConverters<TBuilder>(BuilderSkeleton<TBuilder> builder)
        where TBuilder : BuilderSkeleton<TBuilder>
    {
        builder.WithTypeConverter(new PFxExpressionYamlConverter(Options.PFxExpressionYamlFormatting));
        builder.WithTypeConverter(new NamedObjectYamlConverter());
        builder.WithTypeConverter(new NamedObjectMappingYamlConverter());
    }

    /// <summary>
    /// Applies the formatting options on this instance to a serializer.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "TESTONLY")]
    internal void TESTONLY_ApplySerializerFormatting(SerializerBuilder builder)
    {
        ApplySerializerFormatting(builder);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
