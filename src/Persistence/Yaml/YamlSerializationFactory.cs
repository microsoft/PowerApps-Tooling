// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlSerializationFactory : IYamlSerializationFactory
{
    public ISerializer CreateSerializer()
    {
        var yamlSerializer = new SerializerBuilder()
           .WithFirstClassModels()
           .Build();
        return yamlSerializer;
    }

    public IDeserializer CreateDeserializer()
    {
        var deserializer = new DeserializerBuilder()
           .WithFirstClassModels()
           .Build();
        return deserializer;
    }
}
