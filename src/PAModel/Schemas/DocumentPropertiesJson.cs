//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;

// $$$ Pull in the enums 
using SomeEnum = System.String;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    
    /// <summary>
    /// Schematic class for Properties.json
    /// </summary>
    internal class DocumentPropertiesJson
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public string FileID { get; set; }
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
        public SomeEnum DocumentAppType { get; set; }
        public SomeEnum DocumentType { get; set; }
        public SomeEnum AppCreationSource { get; set; }
        public string AppDescription { get; set; }
        public double? LastControlUniqueId { get; set; }
        public double? DefaultConnectedDataSourceMaxGetRowsCount { get; set; }
        public bool ContainsThirdPartyPcfControls { get; set; } // defaults to false
        public double? ErrorCount { get; set; }
        public string InstrumentationKey { get; set; }
        public bool EnableInstrumentation { get; set; } // default to false
        public Dictionary<string, int> ControlCount { get; set; }
        public double? DeserializationLoadTime { get; set; }
        public double? AnalysisLoadTime { get; set; }
    }
}