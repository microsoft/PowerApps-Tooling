// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(Template)]
internal record Screen : Control
{
    internal const string Template = "Screen";
    public Screen()
    {
        ControlUri = BuiltInTemplatesUris.Screen;
    }
}
