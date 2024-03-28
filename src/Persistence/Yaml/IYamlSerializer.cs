// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

public interface IYamlSerializer
{
    public string SerializeControl<TControl>(TControl graph) where TControl : Control;

    public void SerializeControl<TControl>(TextWriter writer, TControl graph) where TControl : Control;
}
