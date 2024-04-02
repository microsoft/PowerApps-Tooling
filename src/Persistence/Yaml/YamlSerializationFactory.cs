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

    public IYamlSerializer CreateSerializer(bool? isTextFirst = null)
    {
        return CreateSerializer(new YamlSerializerOptions { IsTextFirst = isTextFirst ?? YamlSerializerOptions.Default.IsTextFirst });
    }

    public IYamlSerializer CreateSerializer(YamlSerializerOptions? options)
    {
        options ??= YamlSerializerOptions.Default;

        var yamlSerializer = new SerializerBuilder()
                .WithEventEmitter(next => new FirstClassControlsEmitter(next, _controlTemplateStore))
                .WithTypeInspector(inner => new ControlTypeInspector(inner, _controlTemplateStore))
                .WithTypeConverter(new ControlPropertiesCollectionConverter() { IsTextFirst = options.IsTextFirst })
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
           .Build();

        return new YamlSerializer(yamlSerializer);
    }

    public IYamlDeserializer CreateDeserializer(bool? isTextFirst = null)
    {
        return CreateDeserializer(new YamlDeserializerOptions { IsTextFirst = isTextFirst ?? YamlDeserializerOptions.Default.IsTextFirst });
    }

    public IYamlDeserializer CreateDeserializer(YamlDeserializerOptions? options)
    {
        options ??= YamlDeserializerOptions.Default;

        var yamlDeserializer = new DeserializerBuilder()
            .WithDuplicateKeyChecking()
            .WithObjectFactory(new ControlObjectFactory(_controlTemplateStore, _controlFactory))
            .IgnoreUnmatchedProperties()
            .WithTypeInspector(inner => new ControlTypeInspector(inner, _controlTemplateStore))
            .WithTypeDiscriminatingNodeDeserializer(o =>
            {
                o.AddTypeDiscriminator(new ControlTypeDiscriminator(_controlTemplateStore));
            })
            .WithTypeConverter(new ControlPropertiesCollectionConverter() { IsTextFirst = options.IsTextFirst })
            .Build();

        return new YamlDeserializer(yamlDeserializer);
    }
}
