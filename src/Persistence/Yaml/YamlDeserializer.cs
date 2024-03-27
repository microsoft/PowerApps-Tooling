// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlDeserializer : IYamlDeserializer
{
    private readonly IDeserializer _deserializer;

    internal YamlDeserializer(IDeserializer deserializer)
    {
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public T Deserialize<T>(string yaml) where T : Control
    {
        return _deserializer.Deserialize<T>(yaml);
    }

    public T Deserialize<T>(TextReader reader) where T : Control
    {
        return _deserializer.Deserialize<T>(reader);
    }
}
