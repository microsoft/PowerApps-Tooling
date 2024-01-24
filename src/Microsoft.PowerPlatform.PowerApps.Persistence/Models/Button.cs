// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Button))]
public record Button : Control
{
    public Button()
    {
        ControlUri = BuiltInTemplatesUris.Button;
    }
}
