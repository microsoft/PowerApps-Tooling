// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(nameof(Text))]
public record Text : Control
{
    public Text()
    {
        ControlUri = BuiltInTemplatesUris.Text;
    }
}
