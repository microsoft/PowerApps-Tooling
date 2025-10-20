// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record ThemeDefinition
{
    public required string? BasePaletteColor { get; init; }
    public int? HueTorsion { get; init; }
    public int? Vibrancy { get; init; }
    public bool? ThemePrimaryColorLocked { get; init; }
    public string[]? ColorOverrides { get; init; }
    public string? Font { get; init; }
}
