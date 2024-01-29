// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal static class SerializerBuilderExtensions
{
    public static SerializerBuilder WithFirstClassModels(this SerializerBuilder builder)
    {
        builder = builder.AddAttributeOverrides()
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
        return builder.AddAttributeOverrides()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .WithTypeInspector(inner => new ControlTypeInspector(inner))
            .WithTypeDiscriminatingNodeDeserializer(o =>
            {
                o.AddTypeDiscriminator(new ControlTypeDiscriminator());
            })
            .WithTypeConverter(new ControlPropertiesCollectionConverter());
    }

    private static TBuilder AddAttributeOverrides<TBuilder>(this TBuilder builder)
       where TBuilder : BuilderSkeleton<TBuilder>
    {
        var types = typeof(Control).Assembly.DefinedTypes;
        foreach (var type in types)
        {
            builder = builder
                .WithAttributeOverride(type, nameof(Control.ControlUri), new YamlMemberAttribute() { Order = 0, Alias = YamlFields.Control })
                .WithAttributeOverride(type, nameof(Control.Name), new YamlMemberAttribute() { Order = 1 })
                .WithAttributeOverride(type, nameof(Control.Properties), new YamlMemberAttribute() { Order = 2 })
                .WithAttributeOverride(type, nameof(Control.Controls), new YamlMemberAttribute() { Order = 3 })
                .WithAttributeOverride(type, nameof(Control.EditorState), new YamlIgnoreAttribute());
        }

        return builder;
    }
}
