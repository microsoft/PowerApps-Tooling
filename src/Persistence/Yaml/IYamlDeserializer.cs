// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlDeserializer
{
    public TControl Deserialize<TControl>(string yaml) where TControl : Control;

    public TControl Deserialize<TControl>(TextReader reader) where TControl : Control;
}
