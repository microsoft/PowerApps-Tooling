// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.Extensions;

internal static class YamlDeserializerExtensions
{
    internal static object? DeserializeControl(this IYamlDeserializer deserializer, TextReader yamlReader, Type controlType)
    {
        var deserializeMethod = typeof(IYamlDeserializer).GetMethod(nameof(IYamlDeserializer.Deserialize), types: [typeof(TextReader)])!.MakeGenericMethod(controlType);
        return deserializeMethod.Invoke(deserializer, [yamlReader]);
    }
}
