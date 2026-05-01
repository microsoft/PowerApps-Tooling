// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

/// <summary>
/// Converts any <see cref="NamedObjectMapping{TValue}"/> to and from YAML.
/// The converter is non-generic so a single registration handles all closed <c>TValue</c> types.
/// </summary>
internal sealed class NamedObjectMappingYamlConverter : IYamlTypeConverter
{
    private static readonly ConcurrentDictionary<Type, IYamlTypeConverter> ConverterCache = new();

    public bool Accepts(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NamedObjectMapping<>);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        return GetConverter(type).ReadYaml(parser, type, rootDeserializer);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        GetConverter(type).WriteYaml(emitter, value, type, serializer);
    }

    private static IYamlTypeConverter GetConverter(Type closedMappingType)
    {
        return ConverterCache.GetOrAdd(closedMappingType, static t =>
        {
            var valueType = t.GetGenericArguments()[0];
            var converterType = typeof(NamedObjectMappingYamlConverter<>).MakeGenericType(valueType);
            return (IYamlTypeConverter)Activator.CreateInstance(converterType)!;
        });
    }
}
