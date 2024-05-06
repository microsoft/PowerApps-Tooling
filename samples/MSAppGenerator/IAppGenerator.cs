// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace MSAppGenerator;

/// <summary>
/// Class to implement the interface for the MSApp Generator
/// </summary>
public interface IAppGenerator
{
    /// <summary>
    /// Function to create a MSApp from a set of parameters
    /// </summary>
    App GenerateApp(int numScreens = 1, IList<string>? controls = null);
}
