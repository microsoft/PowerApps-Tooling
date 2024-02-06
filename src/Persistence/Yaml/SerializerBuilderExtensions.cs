// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal static class SerializerBuilderExtensions
{
    public static SerializerBuilder WithFirstClassModels(this SerializerBuilder builder, IControlTemplateStore controlTemplateStore)
    {
        builder = builder
            .WithEventEmitter(next => new FirstClassControlsEmitter(next, controlTemplateStore))
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .WithTypeInspector(inner => new ControlTypeInspector(inner, controlTemplateStore))
            .WithTypeConverter(new ControlPropertyConverter())
            .WithTypeConverter(new ControlPropertiesCollectionConverter())
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull);
        return builder;
    }

    public static DeserializerBuilder WithFirstClassModels(this DeserializerBuilder builder, IControlTemplateStore controlTemplateStore)
    {
        return builder
            .IgnoreUnmatchedProperties()
            .WithTypeInspector(inner => new ControlTypeInspector(inner, controlTemplateStore))
            .WithTypeDiscriminatingNodeDeserializer(o =>
            {
                o.AddTypeDiscriminator(new ControlTypeDiscriminator(controlTemplateStore));
            })
            .WithTypeConverter(new ControlPropertiesCollectionConverter());
    }
}
