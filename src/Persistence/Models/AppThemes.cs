// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

internal sealed record AppThemes
{
    [SetsRequiredMembers]
    public AppThemes()
    {
    }

    public required string CurrentTheme { get; init; } = "defaultTheme";
    public IList<object> CustomThemes { get; init; } = new List<object>();
}
