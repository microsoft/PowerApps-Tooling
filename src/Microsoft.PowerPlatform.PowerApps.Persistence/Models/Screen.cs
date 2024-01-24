// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Screen))]
public record Screen : Control
{
    public Screen()
    {
        ControlUri = BuiltInTemplatesUris.Screen;
    }
}
