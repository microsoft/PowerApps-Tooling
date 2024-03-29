// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlDeserializer : IYamlDeserializer
{
    private readonly IDeserializer _deserializer;

    internal YamlDeserializer(IDeserializer deserializer)
    {
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public T DeserializeControl<T>(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            throw new ArgumentNullException(nameof(yaml));

        return _deserializer.Deserialize<T>(yaml);
    }

    public T DeserializeControl<T>(TextReader reader)
    {
        _ = reader ?? throw new ArgumentNullException(nameof(reader));

        return _deserializer.Deserialize<T>(reader);
    }
}
