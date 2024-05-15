// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3_0;

/// <summary>
/// Represents a group of controls under the same parent.
/// </summary>
public record ControlGroup
{
    public string[] ControlNames { get; init; } = Array.Empty<string>();
}
