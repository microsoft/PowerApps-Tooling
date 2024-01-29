// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(shortName: nameof(Text), templateUri: BuiltInTemplates.Text)]
public record Text : Control
{
    public Text()
    {
        ControlUri = BuiltInTemplates.Text;
    }

    [SetsRequiredMembers]
    public Text(string name) : base(name)
    {
        ControlUri = BuiltInTemplates.Text;
    }
}
