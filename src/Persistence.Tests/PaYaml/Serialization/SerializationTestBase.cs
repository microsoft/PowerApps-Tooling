// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using YamlDotNet.Serialization;

namespace Persistence.Tests.PaYaml.Serialization;

/// <summary>
/// Base class for tests that involve serialization and deserialization of objects using PaYaml.
/// </summary>
public abstract class SerializationTestBase : VSTestBase
{
    protected PaYamlSerializerOptions DefaultOptions { get; set; } = PaYamlSerializerOptions.Default;

    protected string SerializeViaYamlDotNet<T>(T? testObject, PaYamlSerializerOptions? options = null)
        where T : notnull
    {
        options ??= DefaultOptions;

        using var serializationContext = new PaYamlSerializationContext(options);
        var builder = new SerializerBuilder();
        ConfigureYamlDotNetSerializer(builder, serializationContext);
        serializationContext.ValueSerializer = builder.BuildValueSerializer();
        var serializer = builder.Build();

        return serializer.Serialize(testObject);
    }

    protected virtual void ConfigureYamlDotNetSerializer(SerializerBuilder builder, PaYamlSerializationContext context)
    {
        context.TESTONLY_ApplySerializerFormatting(builder);
    }

    protected T? DeserializeViaYamlDotNet<T>(string yaml, PaYamlSerializerOptions? options = null)
        where T : notnull
    {
        options ??= DefaultOptions;

        using var serializationContext = new PaYamlSerializationContext(options);
        var builder = new DeserializerBuilder();
        ConfigureYamlDotNetDeserializer(builder, serializationContext);
        serializationContext.ValueDeserializer = builder.BuildValueDeserializer();
        var deserializer = builder.Build();

        var value = deserializer.Deserialize<T?>(yaml);
        serializationContext.OnDeserialization();
        return value;
    }

    protected virtual void ConfigureYamlDotNetDeserializer(DeserializerBuilder builder, PaYamlSerializationContext context)
    {
        // This should usually sync up with the default configuration in PaYamlSerializerOptions.ApplyToDeserializerBuilder but without type converters
        builder
            .WithDuplicateKeyChecking()
            ;
    }
}
