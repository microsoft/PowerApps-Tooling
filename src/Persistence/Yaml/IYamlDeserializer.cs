// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlDeserializer
{
    /// <exception cref="PersistenceException">Thrown when an error occurs while deserializing.</exception>
    public T? Deserialize<T>(string yaml) where T : notnull;

    /// <exception cref="PersistenceException">Thrown when an error occurs while deserializing.</exception>
    public T? Deserialize<T>(TextReader reader) where T : notnull;
}
