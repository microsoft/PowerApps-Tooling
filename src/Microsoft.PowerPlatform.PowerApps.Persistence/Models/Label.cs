// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Label))]
public record Label : Control
{
    public Label()
    {
        ControlUri = BuiltInTemplatesUris.Label;
    }
}
