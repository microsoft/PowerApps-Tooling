// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;

public record ThemeDefinition
{
    public string? Font { get; init; }
    public required string? BasePaletteColor { get; init; }
    public int HueTorsion { get; init; }
    public int Vibrancy { get; init; }
    public bool IsPrimaryColorLocked { get; init; }
    public ColorOverrides? ColorOverrides { get; init; }
}

public record ColorOverrides
{
    public string? Darker70 { get; init; }
    public string? Darker60 { get; init; }
    public string? Darker50 { get; init; }
    public string? Darker40 { get; init; }
    public string? Darker30 { get; init; }
    public string? Darker20 { get; init; }
    public string? Darker10 { get; init; }
    public string? PrimaryColor { get; init; }
    public string? Lighter10 { get; init; }
    public string? Lighter20 { get; init; }
    public string? Lighter30 { get; init; }
    public string? Lighter40 { get; init; }
    public string? Lighter50 { get; init; }
    public string? Lighter60 { get; init; }
    public string? Lighter70 { get; init; }
    public string? Lighter80 { get; init; }
}
