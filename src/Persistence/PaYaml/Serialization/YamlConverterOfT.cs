// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public abstract class YamlConverter<T>(SerializationContext? serializationContext = null) : IYamlTypeConverter
{
    public Type Type { get; } = typeof(T);

    public SerializationContext SerializationContext { get; } = serializationContext ?? new SerializationContext();

    /// <summary>
    /// The default implementation returns true when <paramref name="type"/> equals typeof(<typeparamref name="T"/>).
    /// </summary>
    public virtual bool Accepts(Type type)
    {
        return type == Type;
    }

    public abstract T ReadYaml(IParser parser, Type typeToConvert);

    public abstract void WriteYaml(IEmitter emitter, T? value, Type typeToConvert);

    object? IYamlTypeConverter.ReadYaml(IParser parser, Type typeToConvert)
    {
        return ReadYaml(parser, typeToConvert);
    }

    void IYamlTypeConverter.WriteYaml(IEmitter emitter, object? value, Type typeToConvert)
    {
        var valueOfT = YamlSerialization.UnboxOnWrite<T>(value);
        WriteYaml(emitter, valueOfT, typeToConvert);
    }
}

internal static class YamlSerialization
{
    [return: NotNullIfNotNull(nameof(value))]
    internal static T? UnboxOnWrite<T>(object? value)
    {
        if (default(T) is not null && value is null)
        {
            // Casting null values to a non-nullable struct throws NullReferenceException.
            throw new InvalidOperationException($"Cannot write null value for non-nullable type {typeof(T).Name}.");
        }

        return (T?)value;
    }

    [return: NotNullIfNotNull(nameof(value))]
    internal static T? UnboxOnRead<T>(object? value)
    {
        if (value is null)
        {
            if (default(T) is not null)
            {
                // Casting null values to a non-nullable struct throws NullReferenceException.
                throw new InvalidOperationException($"Cannot assign null value for non-nullable type {typeof(T).Name}.");
            }

            return default;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidOperationException($"Unable to assign value of type {value.GetType().Name} to type {typeof(T).Name}.");
        //return default!;
    }
}
