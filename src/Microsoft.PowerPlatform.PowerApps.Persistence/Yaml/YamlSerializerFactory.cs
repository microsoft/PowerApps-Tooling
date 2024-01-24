// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public static class YamlSerializerFactory
{
    public static ISerializer Create()
    {
        var yamlSerializer = new SerializerBuilder()
           .WithFirstClassModels()
           .Build();
        return yamlSerializer;
    }
}
