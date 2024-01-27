// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Button), TemplateUri = BuiltInTemplates.Button)]
public record Button : Control
{
    public Button()
    {
        ControlUri = BuiltInTemplates.Button;
    }

    [SetsRequiredMembers]
    public Button(string name) : base(name)
    {
        ControlUri = BuiltInTemplates.Button;
    }
}
