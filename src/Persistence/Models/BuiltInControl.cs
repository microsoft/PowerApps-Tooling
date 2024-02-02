// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public record BuiltInControl : Control
{
    public BuiltInControl()
    {
    }

    [SetsRequiredMembers]
    public BuiltInControl(string name) : base(name)
    {
    }
}
