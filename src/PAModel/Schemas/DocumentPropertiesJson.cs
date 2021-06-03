// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// If we need the values of the enums
// we should pull them in, otherwise, not worth keeping in sync
using SomeEnum = System.String;

namespace Microsoft.AppMagic.Authoring.Persistence
{

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
            var defaultProps = new DocumentPropertiesJson();
            defaultProps.Name = name;
            defaultProps.AppPreviewFlagsKey = GetAppPreviewFlagDefault();
            // This should get it's own app creation source probably, so we can tell from telemetry that it's made from our tool
            defaultProps.AppCreationSource = "AppFromScratch";
            defaultProps.AppDescription = "";
            defaultProps.Author = "";
            defaultProps.FileID = Guid.NewGuid().ToString();
            defaultProps.Id = Guid.NewGuid().ToString();
            defaultProps.ControlCount = new Dictionary<SomeEnum, int>();
            defaultProps.DefaultConnectedDataSourceMaxGetRowsCount = 500;
            defaultProps.DocumentAppType = AppType.DesktopOrTablet;

            // These might need cleaning up for responsive apps?
            defaultProps.DocumentLayoutLockOrientation = true;
            defaultProps.DocumentLayoutMaintainAspectRatio = true;
            defaultProps.DocumentLayoutOrientation = "landscape";
            defaultProps.DocumentLayoutScaleToFit = true;
            defaultProps.DocumentLayoutHeight = 768;
            defaultProps.DocumentLayoutWidth = 1366;

            defaultProps.DocumentType = "App";
            defaultProps.InstrumentationKey = "";
            defaultProps.LibraryDependencies = "[]";
            defaultProps.LocalDatabaseReferences = "[]";

            defaultProps.OriginatingVersion = "1.294";

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
}
