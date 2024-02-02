// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlSerializationFactory : IYamlSerializationFactory
{
    private readonly IControlTemplateStore _controlTemplateStore;

    public YamlSerializationFactory(IControlTemplateStore controlTemplateStore)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
    }

    public ISerializer CreateSerializer()
    {
        var yamlSerializer = new SerializerBuilder()
           .WithFirstClassModels(_controlTemplateStore)
           .Build();
        return yamlSerializer;
    }

    public IDeserializer CreateDeserializer()
    {
        var deserializer = new DeserializerBuilder()
           .WithFirstClassModels(_controlTemplateStore)
           .Build();
        return deserializer;
    }
}
