// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;

/// <summary>
/// An interface for objects that may have a YAML location.
/// This is used to keep a consistent API for objects that may have a location in the YAML file.
/// </summary>
public interface IMayHavePaYamlLocation
{
    /// <summary>
    /// The location in the YAML file where this object starts.
    /// </summary>
    PaYamlLocation? Start { get; }
}
