// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

/// <summary>
/// Strongly-typed converter for <see cref="NamedObjectMapping{TValue}"/>.
/// Typically discovered and dispatched to by the non-generic
/// <see cref="NamedObjectMappingYamlConverter"/>, but can also be registered directly.
/// </summary>
internal sealed class NamedObjectMappingYamlConverter<TValue> : YamlConverter<NamedObjectMapping<TValue>>
    where TValue : notnull
{
    public override NamedObjectMapping<TValue> ReadYaml(IParser parser, Type typeToConvert, ObjectDeserializer rootDeserializer)
    {
        var mapping = new NamedObjectMapping<TValue>();

        if (parser.TryConsumeNull())
        {
            // REVIEW: We may not want to support null scalars when reading named object mappings.
            // For now, treat null inputs as an empty collection (matches prior IYamlConvertible behavior).
            return mapping;
        }

        parser.Consume<MappingStart>();
        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var itemStartEvent = parser.Current!;
            var namedObject = NamedObjectYamlConverter<TValue>.ReadNameAndValueEventsCore(parser, rootDeserializer);

            if (!mapping.TryAdd(namedObject))
            {
                var existingNamedObject = mapping.GetNamedObject(namedObject.Name);
                throw new YamlException(itemStartEvent.Start, itemStartEvent.End, $"Duplicate name '{namedObject.Name}' used at {itemStartEvent}. First use is located at {existingNamedObject.Start}.");
            }
        }

        return mapping;
    }

    public override void WriteYaml(IEmitter emitter, NamedObjectMapping<TValue>? value, Type typeToConvert, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.EmitNull();
            return;
        }

        emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));
        foreach (var namedObject in value)
        {
            NamedObjectYamlConverter<TValue>.WriteNameAndValueEventsCore(emitter, namedObject, serializer);
        }

        emitter.Emit(new MappingEnd());
    }
}
