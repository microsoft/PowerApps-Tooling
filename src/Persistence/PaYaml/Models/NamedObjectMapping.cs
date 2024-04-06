// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// Represents a collection of named objects that are sorted by their names.
/// </summary>
public class NamedObjectMapping<TValue> : NamedObjectMappingBase<string, TValue, NamedObject<TValue>>, INamedObjectCollection<TValue>
    where TValue : notnull
{
    private readonly static StringComparer DefaultComparer = StringComparer.Ordinal;

    public NamedObjectMapping()
        : this(DefaultComparer)
    {
    }

    public NamedObjectMapping(IComparer<string>? comparer)
        : base(comparer ?? DefaultComparer)
    {
    }

    protected override NamedObject<TValue> CreateNamedObject(string name, TValue value)
    {
        _ = name ?? throw new ArgumentNullException(nameof(name));
        _ = value ?? throw new ArgumentNullException(nameof(value));

        return new NamedObject<TValue>(name, value);
    }

    protected override NamedObject<TValue> ReadNamedObjectFromMappingEntryEvents(IParser parser, ObjectDeserializer nestedObjectDeserializer)
    {
        return NamedObjectYamlConverter<TValue>.ReadNameAndValueEventsCore(parser, nestedObjectDeserializer);
    }

    protected override void WriteNamedObjectToMappingEntryEvents(IEmitter emitter, NamedObject<TValue> namedObject, ObjectSerializer nestedObjectSerializer)
    {
        NamedObjectYamlConverter<TValue>.WriteNameAndValueEventsCore(emitter, namedObject, nestedObjectSerializer);
    }
}
