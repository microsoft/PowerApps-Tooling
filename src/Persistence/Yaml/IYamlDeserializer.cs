// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlDeserializer
{
    public T DeserializeControl<T>(string yaml);

    public T DeserializeControl<T>(TextReader reader);
}
