// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

internal class NamedObjectYamlConverter<TValue> : YamlConverter<NamedObject<TValue>>
    where TValue : notnull
{
    internal static NamedObject<TValue> ReadNameAndValueEventsCore(IParser parser, ObjectDeserializer rootDeserializer)
    {
        _ = parser.Current ?? throw new InvalidOperationException("The parser has not been started or has nothing to read.");
        _ = rootDeserializer ?? throw new ArgumentNullException(nameof(rootDeserializer));

        // The default representation is expected to represent a single-item mapping
        var start = parser.Current.Start;
        var name = (string?)rootDeserializer(typeof(string)) ?? throw new YamlException(start, parser.Current.End, $"Named object key cannot be null.");
        var value = (TValue?)rootDeserializer(typeof(TValue)) ?? throw new YamlException(start, parser.Current.End, $"Named object value cannot be null.");

        return new NamedObject<TValue>(name, value) { Start = PaYamlLocation.FromMark(start) };
    }

    internal static void WriteNameAndValueEventsCore(IEmitter emitter, NamedObject<TValue> namedObject, ObjectSerializer serializer)
    {
        _ = emitter ?? throw new ArgumentNullException(nameof(emitter));
        _ = namedObject ?? throw new ArgumentNullException(nameof(namedObject));
        _ = serializer ?? throw new ArgumentNullException(nameof(serializer));

        // Only write the events for the mapping key/value.
        serializer(namedObject.Name, typeof(string));
        serializer(namedObject.Value, typeof(TValue));
    }

    public override NamedObject<TValue> ReadYaml(IParser parser, Type typeToConvert, ObjectDeserializer rootDeserializer)
    {
        // The default representation is expected to represent a single-item mapping
        var mappingStart = parser.Consume<MappingStart>();
        var namedObject = ReadNameAndValueEventsCore(parser, rootDeserializer);

        // There shouldn't be any more keys in the mapping
        parser.Consume<MappingEnd>();

        return namedObject;
    }

    public override void WriteYaml(IEmitter emitter, NamedObject<TValue>? value, Type typeToConvert, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.EmitNull();
            return;
        }

        // The default representation is expected to represent a single-item mapping
        emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));

        WriteNameAndValueEventsCore(emitter, value, serializer);

        emitter.Emit(new MappingEnd());
    }
}
