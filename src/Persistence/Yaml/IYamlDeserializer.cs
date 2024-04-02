// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlDeserializer
{
    public T Deserialize<T>(string yaml);

    public T Deserialize<T>(TextReader reader);
}
