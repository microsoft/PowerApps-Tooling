// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

// BUG 27469059: Internal classes not accessible to test project. InternalsVisibleTo attribute added to csproj doesn't get emitted because GenerateAssemblyInfo is false.
public class NamedObjectYamlConverter<TValue> : YamlConverter<NamedObject<TValue>>
    where TValue : notnull
{
    public NamedObjectYamlConverter(PaYamlSerializationContext context)
        : base(context)
    {
    }

    internal static NamedObject<TValue> ReadNameAndValueEventsCore(IParser parser, ObjectDeserializer nestedObjectDeserializer)
    {
        _ = parser.Current ?? throw new InvalidOperationException("The parser has not been started or has nothing to read.");
        _ = nestedObjectDeserializer ?? throw new ArgumentNullException(nameof(nestedObjectDeserializer));

        // The default representation is expected to represent a single-item mapping
        var start = parser.Current.Start;
        var name = (string?)nestedObjectDeserializer(typeof(string)) ?? throw new YamlException(start, parser.Current.End, $"Named object key cannot be null.");
        var value = (TValue?)nestedObjectDeserializer(typeof(TValue)) ?? throw new YamlException(start, parser.Current.End, $"Named object value cannot be null.");

        return new NamedObject<TValue>(name, value) { Start = PaYamlLocation.FromMark(start) };
    }

    internal static void WriteNameAndValueEventsCore(IEmitter emitter, NamedObject<TValue> namedObject, ObjectSerializer nestedObjectSerializer)
    {
        _ = emitter ?? throw new ArgumentNullException(nameof(emitter));
        _ = namedObject ?? throw new ArgumentNullException(nameof(namedObject));
        _ = nestedObjectSerializer ?? throw new ArgumentNullException(nameof(nestedObjectSerializer));

        // Only write the events for the mapping key/value.
        nestedObjectSerializer(namedObject.Name, typeof(string));
        nestedObjectSerializer(namedObject.Value, typeof(TValue));
    }

    public override NamedObject<TValue> ReadYaml(IParser parser, Type typeToConvert)
    {
        // The default representation is expected to represent a single-item mapping
        var mappingStart = parser.Consume<MappingStart>();
        var namedObject = ReadNameAndValueEventsCore(parser, SerializationContext.CreateObjectDeserializer(parser));

        // There shouldn't be any more keys in the mapping
        parser.Consume<MappingEnd>();

        return namedObject;
    }

    public override void WriteYaml(IEmitter emitter, NamedObject<TValue>? value, Type typeToConvert)
    {
        if (value is null)
        {
            emitter.EmitNull();
            return;
        }

        // The default representation is expected to represent a single-item mapping
        emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));

        WriteNameAndValueEventsCore(emitter, value, SerializationContext.CreateObjectSerializer(emitter));

        emitter.Emit(new MappingEnd());
    }
}
