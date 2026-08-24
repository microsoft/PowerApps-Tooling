// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

internal interface ISetPaYamlNodeLocation : IMayHavePaYamlLocation
{
    /// <summary>
    /// Sets the location of the node in the YAML file.
    /// </summary>
    void SetNodeLocation(PaYamlLocation? start);
}
