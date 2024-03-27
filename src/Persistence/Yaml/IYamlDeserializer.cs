// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlDeserializer
{
    public T Deserialize<T>(string yaml) where T : Control;

    public T Deserialize<T>(TextReader reader) where T : Control;
}
