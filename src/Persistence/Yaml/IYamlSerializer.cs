// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializer
{
    public string Serialize<T>(T graph) where T : Control;

    public void Serialize<T>(TextWriter writer, T graph) where T : Control;
}
