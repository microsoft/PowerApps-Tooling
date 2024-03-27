// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public class YamlDeserializer : IYamlDeserializer
{
    private readonly IDeserializer _deserializer;
    private readonly IControlFactory _controlFactory;

    internal YamlDeserializer(IDeserializer deserializer, IControlFactory controlFactory)
    {
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));
    }

    public TControl DeserializeControl<TControl>(string yaml) where TControl : Control
    {
        if (string.IsNullOrWhiteSpace(yaml))
            throw new ArgumentNullException(nameof(yaml));

        return _deserializer.Deserialize<TControl>(yaml).AfterDeserialize(_controlFactory);
    }

    public TControl DeserializeControl<TControl>(TextReader reader) where TControl : Control
    {
        _ = reader ?? throw new ArgumentNullException(nameof(reader));

        return _deserializer.Deserialize<TControl>(reader).AfterDeserialize(_controlFactory);
    }
}
