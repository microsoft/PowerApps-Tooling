// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializer
{
    /// <exception cref="PersistenceLibraryException">Thrown when an error occurs while serializing.</exception>
    public string Serialize(object graph);

    /// <exception cref="PersistenceLibraryException">Thrown when an error occurs while serializing.</exception>
    public string SerializeControl<T>(T graph) where T : Control;

    /// <exception cref="PersistenceLibraryException">Thrown when an error occurs while serializing.</exception>
    public void SerializeControl<T>(TextWriter writer, T graph) where T : Control;
}
