// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(shortName: nameof(Screen), templateUri: BuiltInTemplates.Screen)]
public record Screen : Control
{
    public Screen()
    {
        ControlUri = BuiltInTemplates.Screen;
    }

    [SetsRequiredMembers]
    public Screen(string name) : base(name)
    {
        ControlUri = BuiltInTemplates.Screen;
    }
}
