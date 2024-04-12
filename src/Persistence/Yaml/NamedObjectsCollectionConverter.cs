// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class NamedObjectsCollectionConverter<T> : IYamlTypeConverter
    where T : class, INamedObject
{
    public required YamlSerializationOptions Options { get; set; }

    public IValueDeserializer? ValueDeserializer { get; set; }

    public IValueSerializer? ValueSerializer { get; set; }

    public bool Accepts(Type type)
    {
        return
            type == typeof(List<T>) || type.IsSubclassOf(typeof(List<T>)) ||
            type == typeof(IList<T>) || type.IsSubclassOf(typeof(IList<T>)) ||
            type == typeof(T[]);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        if (parser.Current is not SequenceStart)
            throw new YamlException(parser.Current!.Start, parser.Current.End, $"Expected sequence of {typeof(T).Name} to start but got {parser.Current.GetType().Name}");

        if (!parser.MoveNext())
            throw new YamlException(parser.Current.Start, parser.Current.End, $"Expected start of {typeof(T).Name}");

        var collection = new List<T>();
        while (!parser.Accept<SequenceEnd>(out _))
        {
            var name = string.Empty;
            if (Options.IsControlIdentifiers)
            {
                if (parser.Current is not MappingStart)
                    throw new YamlException(parser.Current!.Start, parser.Current.End, $"Expected mapping of {typeof(T).Name} to start but got {parser.Current.GetType().Name}");
                if (!parser.MoveNext())
                    throw new YamlException(parser.Current.Start, parser.Current.End, $"Expected definition of {typeof(T).Name}");
                if (parser.Current is not Scalar)
                    throw new YamlException(parser.Current!.Start, parser.Current.End, $"Expected name of {typeof(T).Name}");

                name = parser.Consume<Scalar>().Value;
            }

            using var serializerState = new SerializerState();
            var controlObj = ValueDeserializer!.DeserializeValue(parser, typeof(T), serializerState, ValueDeserializer);
            if (controlObj is not T namedObject)
                throw new YamlException(parser.Current.Start, parser.Current.End, $"Expected {typeof(T).Name}");

            if (Options.IsControlIdentifiers)
            {
                namedObject.Name = name;
                parser.Consume<MappingEnd>();
            }
            collection.Add(namedObject);
        }

        parser.MoveNext();

        return collection;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        if (value == null)
            return;

        var collection = (IEnumerable<T>)value!;

        emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));

        foreach (var key in collection)
        {
            if (Options.IsControlIdentifiers)
            {
                emitter.Emit(new MappingStart());
                emitter.Emit(new Scalar(key.Name));
            }

            ValueSerializer!.SerializeValue(emitter, key, typeof(T));

            if (Options.IsControlIdentifiers)
                emitter.Emit(new MappingEnd());
        }

        emitter.Emit(new SequenceEnd());
    }
}
