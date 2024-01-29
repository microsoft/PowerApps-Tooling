// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record CustomControl : Control
{
    public CustomControl()
    {
    }

    [SetsRequiredMembers]
    public CustomControl(string name) : base(name)
    {
    }
}
