// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Button))]
public record Button : Control
{
    public Button()
    {
        ControlUri = BuiltInTemplatesUris.Button;
    }
}
