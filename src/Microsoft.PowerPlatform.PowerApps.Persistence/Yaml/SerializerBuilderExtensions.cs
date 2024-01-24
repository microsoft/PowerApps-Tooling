// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using YamlDotNet.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

internal static class SerializerBuilderExtensions
{
    public static SerializerBuilder WithFirstClassModels(this SerializerBuilder builder)
    {
        var types = typeof(Control).Assembly.DefinedTypes;
        foreach (var type in types)
        {
            Attribute newAttrib;

            var controlAttrib = type.GetCustomAttributes(true).FirstOrDefault(a => a is FirstClassAttribute) as FirstClassAttribute;
            if (controlAttrib is not null)
                newAttrib = new YamlIgnoreAttribute();
            else
                newAttrib = new YamlMemberAttribute(typeof(string))
                {
                    Alias = "Control",
                    Order = 0,
                };

            builder = builder.WithAttributeOverride(type, nameof(Control.ControlUri), newAttrib)
                .WithAttributeOverride(type, nameof(Control.Name), new YamlMemberAttribute() { Order = 1 })
                .WithAttributeOverride(type, nameof(Control.Properties), new YamlMemberAttribute() { Order = 2 })
                .WithAttributeOverride(type, nameof(Control.Controls), new YamlMemberAttribute() { Order = 3 });
        }

        builder = builder
            .WithEventEmitter(next => new FirstClassControlsEmitter(next))
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .WithTypeConverter(new ControlPropertyConverter())
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull);

        return builder;
    }
}
