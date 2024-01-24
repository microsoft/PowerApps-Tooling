// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(Template)]
internal record Label : Control
{
    internal const string Template = "Label";
    public Label()
    {
        ControlUri = BuiltInTemplatesUris.Label;
    }
}
