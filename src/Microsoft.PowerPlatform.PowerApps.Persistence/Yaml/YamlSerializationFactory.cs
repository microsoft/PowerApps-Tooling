// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public static class YamlSerializationFactory
{
    public static ISerializer CreateSerializer()
    {
        var yamlSerializer = new SerializerBuilder()
           .WithFirstClassModels()
           .Build();
        return yamlSerializer;
    }

    public static IDeserializer CreateDeserializer()
    {
        var deserializer = new DeserializerBuilder()
           .WithFirstClassModels()
           .Build();
        return deserializer;
    }
}
