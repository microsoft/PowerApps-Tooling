// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// App properties
/// </summary>
public record AppProperties
{
    [SetsRequiredMembers]
    public AppProperties()
    {
        // Default values for AppPreviewFlags
        PreviewFlags = new() {
            { "datatablev2control", true },
            { "delayloadscreens", true },
            { "enableonstart", true },
            { "enablepcfmoderndatasets", true },
            { "enablesaveloadcleardataonweb", true },
            { "errorhandling", true },
            { "expandedsavedatasupport", true },
            { "fluentv9controlspreview", true },
            { "formuladataprefetch", true },
            { "reactformulabar", true },
            { "usenonblockingonstartrule", true },
        };
    }

    public required string Author { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;
    public required string Id { get; init; } = Guid.NewGuid().ToString();
    public string? FileID { get; init; }
    public required string LocalConnectionReferences { get; init; } = string.Empty;
    public required string LocalDatabaseReferences { get; init; } = string.Empty;
    public string? LibraryDependencies { get; init; }
    public string[] AppPreviewFlagsKey { get; init; } = [];
    [JsonPropertyName("AppPreviewFlagsMap")]
    public required Dictionary<string, bool> PreviewFlags { get; init; }
    public double? DocumentLayoutWidth { get; init; }
    public double? DocumentLayoutHeight { get; init; }
    public string? DocumentLayoutOrientation { get; init; }
    public bool? DocumentLayoutScaleToFit { get; init; }
    public bool? DocumentLayoutMaintainAspectRatio { get; init; }
    public bool? DocumentLayoutLockOrientation { get; init; }
    public bool? ShowStatusBar { get; init; }
    public string? OriginatingVersion { get; init; }
    public string DocumentAppType { get; init; } = "DesktopOrTablet";
    public string DocumentType { get; init; } = "App";
    public string? AppCreationSource { get; init; } = "AppFromScratch";
    public string? AppDescription { get; init; }
    public double? LastControlUniqueId { get; init; }
    public double? DefaultConnectedDataSourceMaxGetRowsCount { get; init; }
    public bool ContainsThirdPartyPcfControls { get; init; }
    public double? ParserErrorCount { get; init; }
    public double? BindingErrorCount { get; init; }
    public string? InstrumentationKey { get; init; }
    public bool EnableInstrumentation { get; init; }
    public required Dictionary<string, int> ControlCount { get; init; } = new();
    public double? DeserializationLoadTime { get; init; }
    public double? AnalysisLoadTime { get; init; }
    public string? ManualOfflineProfileId { get; init; }
}
