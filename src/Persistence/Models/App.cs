// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// Represents an Canvas App.
/// </summary>
[FirstClass(shortName: nameof(App), templateUri: BuiltInTemplates.AppInfo)]
public record App : Control
{
    public App()
    {
        ControlUri = BuiltInTemplates.AppInfo;
    }

    [SetsRequiredMembers]
    public App(string name) : base(name)
    {
        ControlUri = BuiltInTemplates.AppInfo;
    }

    public IList<Screen> Screens { get; set; } = new List<Screen>();
}
