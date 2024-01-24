// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(Template)]
internal record Button : Control
{
    internal const string Template = "Button";
    public Button()
    {
        ControlUri = BuiltInTemplatesUris.Button;
    }
}
