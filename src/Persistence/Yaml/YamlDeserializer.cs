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

    public TControl Deserialize<TControl>(string yaml) where TControl : Control
    {
        return _deserializer.Deserialize<TControl>(yaml).AfterDeserialize(_controlFactory);
    }

    public TControl Deserialize<TControl>(TextReader reader) where TControl : Control
    {
        return _deserializer.Deserialize<TControl>(reader).AfterDeserialize(_controlFactory);
    }
}
