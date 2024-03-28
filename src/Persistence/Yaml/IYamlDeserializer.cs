// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlDeserializer
{
    public TControl DeserializeControl<TControl>(string yaml) where TControl : Control;

    public TControl DeserializeControl<TControl>(TextReader reader) where TControl : Control;
}
