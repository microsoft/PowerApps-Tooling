// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal static class SerializerBuilderExtensions
{
    public static SerializerBuilder WithFirstClassModels(this SerializerBuilder builder)
    {
        builder = builder
            .WithEventEmitter(next => new FirstClassControlsEmitter(next))
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .WithTypeInspector(inner => new ControlTypeInspector(inner))
            .WithTypeConverter(new ControlPropertyConverter())
            .WithTypeConverter(new ControlPropertiesCollectionConverter())
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull);

        return builder;
    }

    public static DeserializerBuilder WithFirstClassModels(this DeserializerBuilder builder)
    {
        return builder
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .WithTypeInspector(inner => new ControlTypeInspector(inner))
            .WithTypeDiscriminatingNodeDeserializer(o =>
            {
                o.AddTypeDiscriminator(new ControlTypeDiscriminator());
            })
            .WithTypeConverter(new ControlPropertiesCollectionConverter());
    }
}
