// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

public class SerializationContext
{
    // BUG: Make these setters internal once InternalsVisibleTo tests work.
    public IValueSerializer? ValueSerializer { get; set; }
    public IValueDeserializer? ValueDeserializer { get; set; }

    public ObjectDeserializer CreateObjectDeserializer(IParser parser, SerializerState serializerState)
    {
        var valueDeserializer = ValueDeserializer ?? throw new InvalidOperationException($"{nameof(ValueDeserializer)} is not set.");

        return (t) => valueDeserializer.DeserializeValue(parser, t, serializerState, valueDeserializer);
    }

    public ObjectSerializer CreateObjectSerializer(IEmitter emitter)
    {
        var valueSerializer = ValueSerializer ?? throw new InvalidOperationException($"{nameof(ValueSerializer)} is not set.");

        return (v, t) => valueSerializer.SerializeValue(emitter, v, t);
    }

    //public T? DeserializeNestedObject<T>()
    //    where T : notnull
    //{
    //    _ = NestedObjectDeserializer ?? throw new InvalidOperationException($"{nameof(NestedObjectDeserializer)} is not set.");

    //    return (T?)NestedObjectDeserializer(typeof(T));
    //}

    //public void SerializeNestedObject<T>(T? value, Type? type = null)
    //    where T : notnull
    //{
    //    _ = NestedObjectSerializer ?? throw new InvalidOperationException($"{nameof(NestedObjectSerializer)} is not set.");

    //    NestedObjectSerializer(value, typeof(T));
    //}
}
