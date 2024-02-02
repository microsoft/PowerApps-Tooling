// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

[DebuggerDisplay("{Name}")]
public record ControlTemplate
{
    public required string Name { get; init; }
    public required string Uri { get; init; }
}
