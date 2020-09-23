// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// $$$ Pull in the enums 
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
        public double? LastControlUniqueId { get; set; }
        public double? DefaultConnectedDataSourceMaxGetRowsCount { get; set; }
        public bool ContainsThirdPartyPcfControls { get; set; } // defaults to false
        public double? ErrorCount { get; set; }
        public string InstrumentationKey { get; set; }
        public Dictionary<string, int> ControlCount { get; set; }
        public double? DeserializationLoadTime { get; set; }
        public double? AnalysisLoadTime { get; set; }

        // Keys that are optional, or added later (and may or may not appear) will be captured here.
        // public bool EnableInstrumentation { get; set; } // default to false
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
    }
}
