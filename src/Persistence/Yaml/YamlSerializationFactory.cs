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

    public IYamlSerializer CreateSerializer(YamlSerializationOptions? options = null)
    {
        options ??= YamlSerializationOptions.Default;

        var componentConverter = new ComponentConverter(_controlFactory) { Options = options };
        var controlConverter = new ControlConverter(_controlFactory) { Options = options };
        var customPropertiesCollectionConverter = new CustomPropertiesCollectionConverter() { Options = options };

        var builder = new SerializerBuilder()
            .WithTypeInspector(inner => new ControlTypeInspector(inner, _controlTemplateStore))
            .WithTypeConverter(new ControlPropertiesCollectionConverter() { Options = options })
            .WithTypeConverter(controlConverter)
            .WithTypeConverter(componentConverter)
            .WithTypeConverter(customPropertiesCollectionConverter)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull);

        var valueSerializer = builder.BuildValueSerializer();
        componentConverter.ValueSerializer = valueSerializer;
        controlConverter.ValueSerializer = valueSerializer;
        customPropertiesCollectionConverter.ValueSerializer = valueSerializer;

        return new YamlSerializer(builder.Build());
    }

    public IYamlDeserializer CreateDeserializer(YamlSerializationOptions? options = null)
    {
        options ??= YamlSerializationOptions.Default;

        var builder = new DeserializerBuilder()
            .WithDuplicateKeyChecking()
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new ControlPropertiesCollectionConverter() { Options = options });

        if (!options.IsControlIdentifiers)
        {
            builder
                .WithObjectFactory(new ControlObjectFactory(_controlTemplateStore, _controlFactory))
                .WithTypeDiscriminatingNodeDeserializer(o =>
                {
                    o.AddTypeDiscriminator(new ControlTypeDiscriminator(_controlTemplateStore));
                });
        }

        var controlConverter = new ControlConverter(_controlFactory) { Options = options };
        var componentConverter = new ComponentConverter(_controlFactory) { Options = options };
        var appConverter = new AppConverter(_controlFactory) { Options = options };
        var controlCollectionConverter = new ControlCollectionConverter()
        {
            IsTextFirst = options.IsTextFirst
        };
        var customPropertiesCollectionConverter = new CustomPropertiesCollectionConverter() { Options = options };

        // Order of type converters is important
        builder
            .WithTypeConverter(new ControlPropertyConverter())
            .WithTypeConverter(controlConverter)
            .WithTypeConverter(componentConverter)
            .WithTypeConverter(appConverter)
            .WithTypeConverter(controlCollectionConverter)
            .WithTypeConverter(customPropertiesCollectionConverter);

        // We need to build the value deserializer after adding the converters
        var valueDeserializer = builder.BuildValueDeserializer();
        customPropertiesCollectionConverter.ValueDeserializer = valueDeserializer;
        controlConverter.ValueDeserializer = valueDeserializer;
        componentConverter.ValueDeserializer = valueDeserializer;
        appConverter.ValueDeserializer = valueDeserializer;
        controlCollectionConverter.ValueDeserializer = valueDeserializer;

        return new YamlDeserializer(builder.Build());
    }
}
