// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.PowerFx;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public class PaYamlSerializationContext(PaYamlSerializerOptions options) : IDisposable
{
    private readonly SerializerState _serializerState = new();
    private bool _isDisposed;

    /// <summary>
    /// The options used when creating this context.
    /// </summary>
    public PaYamlSerializerOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));

    // BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
    public IValueSerializer? ValueSerializer { get; set; }

    // BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
    public IValueDeserializer? ValueDeserializer { get; set; }

    public ObjectDeserializer CreateObjectDeserializer(IParser parser)
    {
        var valueDeserializer = ValueDeserializer ?? throw new InvalidOperationException($"{nameof(ValueDeserializer)} is not set.");

        return (t) => valueDeserializer.DeserializeValue(parser, t, _serializerState, valueDeserializer);
    }

    public ObjectSerializer CreateObjectSerializer(IEmitter emitter)
    {
        var valueSerializer = ValueSerializer ?? throw new InvalidOperationException($"{nameof(ValueSerializer)} is not set.");

        return (v, t) => valueSerializer.SerializeValue(emitter, v, t);
    }

    // BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
    public void OnDeserialization()
    {
        _serializerState.OnDeserialization();
    }

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
        builder.WithTypeConverter(new NamedObjectYamlConverter<ControlInstance>(this));
        builder.WithTypeConverter(new NamedObjectYamlConverter<PFxFunctionParameter>(this));
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _serializerState.Dispose();
            }

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
