// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

// If we need the values of the enums
// we should pull them in, otherwise, not worth keeping in sync
using SomeEnum = System.String;

namespace Microsoft.AppMagic.Authoring.Persistence;

internal enum AppType
{
    DesktopOrTablet = 0,
    Phone = 1,
    Web = 2,
}

/// <summary>
/// Schematic class for Properties.json
/// </summary>
internal class DocumentPropertiesJson
{
    public string Author { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public string FileID { get; set; }

    // Stores the connections 
    // Dictionary-->Connection object 
    public string LocalConnectionReferences { get; set; }

    public string LocalDatabaseReferences { get; set; }

    // stores double json encoded list. Array of ComponentDependencyInfo
    public string LibraryDependencies { get; set; }

    public string[] AppPreviewFlagsKey { get; set; }
    public double? DocumentLayoutWidth { get; set; }
    public double? DocumentLayoutHeight { get; set; }
    public SomeEnum DocumentLayoutOrientation { get; set; }
    public bool? DocumentLayoutScaleToFit { get; set; }
    public bool? DocumentLayoutMaintainAspectRatio { get; set; }
    public bool? DocumentLayoutLockOrientation { get; set; }
    public string OriginatingVersion { get; set; }
    public AppType DocumentAppType { get; set; }
    public SomeEnum DocumentType { get; set; }
    public SomeEnum AppCreationSource { get; set; }
    public string AppDescription { get; set; }
    public double? DefaultConnectedDataSourceMaxGetRowsCount { get; set; }
    public string InstrumentationKey { get; set; }
    public Dictionary<string, int> ControlCount { get; set; }
    public double? DeserializationLoadTime { get; set; }
    public double? AnalysisLoadTime { get; set; }
    public double? ErrorCount { get; set; }
    // Keys that are optional, or added later (and may or may not appear) will be captured here.
    // public bool EnableInstrumentation { get; set; } // default to false
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }

    public static DocumentPropertiesJson CreateDefault(string name)
    {
        var defaultProps = new DocumentPropertiesJson
        {
            Name = name,
            AppPreviewFlagsKey = GetAppPreviewFlagDefault(),
            // This should get it's own app creation source probably, so we can tell from telemetry that it's made from our tool
            AppCreationSource = "AppFromScratch",
            AppDescription = "",
            Author = "",
            FileID = Guid.NewGuid().ToString(),
            Id = Guid.NewGuid().ToString(),
            ControlCount = new Dictionary<SomeEnum, int>(),
            DefaultConnectedDataSourceMaxGetRowsCount = 500,
            DocumentAppType = AppType.DesktopOrTablet,

            // These might need cleaning up for responsive apps?
            DocumentLayoutLockOrientation = true,
            DocumentLayoutMaintainAspectRatio = true,
            DocumentLayoutOrientation = "landscape",
            DocumentLayoutScaleToFit = true,
            DocumentLayoutHeight = 768,
            DocumentLayoutWidth = 1366,

            DocumentType = "App",
            InstrumentationKey = "",
            LibraryDependencies = "[]",
            LocalDatabaseReferences = "[]",

            OriginatingVersion = "1.294"
        };

        return defaultProps;
    }

    private static string[] GetAppPreviewFlagDefault()
    {
        return new string[]
        {
          "delayloadscreens",
          "blockmovingcontrol",
          "projectionmapping",
          "usedisplaynamemetadata",
          "usenonblockingonstartrule",
          "useguiddatatypes",
          "useexperimentalcdsconnector",
          "useenforcesavedatalimits",
          "componentauthoring",
          "reliableconcurrent",
          "dataTableV2Control",
          "nativecdsexperimental",
          "useexperimentalsqlconnector",
          "enablecdsfileandlargeimage",
          "enhanceddelegation",
          "aibuilderserviceenrollment",
          "enablesummerlandgeospatialfeatures",
          "enablesummerlandmixedrealityfeatures"
        };
    }
}

internal class LocalDatabaseReferenceJson
{
    public Dictionary<string, LocalDatabaseReferenceDataSource> dataSources { get; set; }
    public string instanceUrl { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}

internal class LocalDatabaseReferenceDataSource
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }
}
