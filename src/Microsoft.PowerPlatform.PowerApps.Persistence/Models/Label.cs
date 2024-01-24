// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Label))]
public record Label : Control
{
    public Label()
    {
        ControlUri = BuiltInTemplatesUris.Label;
    }
}
