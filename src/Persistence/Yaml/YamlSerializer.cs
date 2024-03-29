// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlSerializer : IYamlSerializer
{
    private readonly ISerializer _serializer;

    internal YamlSerializer(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public string SerializeControl<T>(T graph)
    {
        return _serializer.Serialize(graph);
    }

    public void SerializeControl<T>(TextWriter writer, T graph)
    {
        _serializer.Serialize(writer, graph);
    }
}
