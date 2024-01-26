// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Screen))]
public record Screen : Control
{
    public Screen()
    {
        ControlUri = BuiltInTemplatesUris.Screen;
    }
}
