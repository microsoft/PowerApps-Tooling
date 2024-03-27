// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlSerializer : IYamlSerializer
{
    private readonly ISerializer _serializer;

    internal YamlSerializer(ISerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public string Serialize<TControl>(TControl graph) where TControl : Control
    {
        return _serializer.Serialize(graph.BeforeSerialize());
    }

    public void Serialize<TControl>(TextWriter writer, TControl graph) where TControl : Control
    {
        _serializer.Serialize(writer, graph.BeforeSerialize());
    }
}
