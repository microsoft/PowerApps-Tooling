// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlSerializationFactory : IYamlSerializationFactory
{
    private readonly IControlTemplateStore _controlTemplateStore;
    private readonly IControlFactory _controlFactory;

    public YamlSerializationFactory(IControlTemplateStore controlTemplateStore, IControlFactory controlFactory)
    {
        _controlTemplateStore = controlTemplateStore ?? throw new ArgumentNullException(nameof(controlTemplateStore));
        _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));
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
        var yamlDeserializer = new DeserializerBuilder()
           .WithObjectFactory(new ControlObjectFactory(_controlTemplateStore, _controlFactory))
           .WithFirstClassModels(_controlTemplateStore)
           .Build();
        return yamlDeserializer;
    }
}
