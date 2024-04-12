// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
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
        var customPropertiesCollectionConverter = new NamedObjectsCollectionConverter<CustomProperty>() { Options = options };
        var customPropertyParametersCollectionConverter = new NamedObjectsCollectionConverter<CustomPropertyParameter>() { Options = options };

        var builder = new SerializerBuilder()
            .WithTypeConverter(new ControlPropertiesCollectionConverter() { Options = options })
            .WithTypeConverter(controlConverter)
            .WithTypeConverter(componentConverter)
            .WithTypeConverter(customPropertiesCollectionConverter)
            .WithTypeConverter(customPropertyParametersCollectionConverter)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull);

        if (options.IsControlIdentifiers)
            builder.WithTypeInspector(inner => new NamedObjectTypeInspector(inner));
        else
            builder.WithTypeInspector(inner => new ControlTypeInspector(inner, _controlTemplateStore));

        var valueSerializer = builder.BuildValueSerializer();
        componentConverter.ValueSerializer = valueSerializer;
        controlConverter.ValueSerializer = valueSerializer;
        customPropertiesCollectionConverter.ValueSerializer = valueSerializer;
        customPropertyParametersCollectionConverter.ValueSerializer = valueSerializer;

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
        var customPropertiesCollectionConverter = new NamedObjectsCollectionConverter<CustomProperty>() { Options = options };
        var customPropertyParametersCollectionConverter = new NamedObjectsCollectionConverter<CustomPropertyParameter>() { Options = options };
        var controlCollectionConverter = new ControlCollectionConverter()
        {
            IsTextFirst = options.IsTextFirst
        };

        // Order of type converters is important
        builder
            .WithTypeConverter(new ControlPropertyConverter())
            .WithTypeConverter(controlConverter)
            .WithTypeConverter(componentConverter)
            .WithTypeConverter(appConverter)
            .WithTypeConverter(customPropertiesCollectionConverter)
            .WithTypeConverter(customPropertyParametersCollectionConverter)
            .WithTypeConverter(controlCollectionConverter);

        // We need to build the value deserializer after adding the converters
        var valueDeserializer = builder.BuildValueDeserializer();
        controlConverter.ValueDeserializer = valueDeserializer;
        componentConverter.ValueDeserializer = valueDeserializer;
        appConverter.ValueDeserializer = valueDeserializer;
        customPropertiesCollectionConverter.ValueDeserializer = valueDeserializer;
        customPropertyParametersCollectionConverter.ValueDeserializer = valueDeserializer;
        controlCollectionConverter.ValueDeserializer = valueDeserializer;

        return new YamlDeserializer(builder.Build());
    }
}
