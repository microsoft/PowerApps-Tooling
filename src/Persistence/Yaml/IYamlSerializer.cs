// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializer
{
    public string SerializeControl<T>(T graph);

    public void SerializeControl<T>(TextWriter writer, T graph);
}
