// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record Settings
{
    public enum AppLayout
    {
        Portrait,
        Landscape
    }

    public string Name { get; set; } = string.Empty;
    public AppLayout Layout { get; set; }
}
