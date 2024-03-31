// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializer
{
    public string Serialize(object graph);

    public string SerializeControl<T>(T graph) where T : Control;

    public void SerializeControl<T>(TextWriter writer, T graph) where T : Control;
}
