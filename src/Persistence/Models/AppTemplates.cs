// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// App Templates
/// </summary>
internal record AppTemplates
{
    [SetsRequiredMembers]
    public AppTemplates()
    {
    }

    public required IList<object> UsedTemplates { get; init; } = new List<object>();
    public required IList<object>? ComponentTemplates { get; init; }
    public required IList<object>? PcfTemplates { get; init; }
}
