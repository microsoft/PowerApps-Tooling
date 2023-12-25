// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;

namespace Microsoft.PowerPlatform.Formulas.Tools;

// Manifest combines the various property/header/publish files into one. 
class CanvasManifestJson
{
    // Format version. 
    // this is most critical for versioning. 
    public Version FormatVersion { get; set; }

    // Issue#21: Server Side changes should address noise in Properties.json
    public DocumentPropertiesJson Properties { get; set; }

    // SavedDate
    public HeaderJson Header { get; set; }

    // Logo file
    // $$$ Other files?
    public PublishInfoJson PublishInfo { get; set; }
    public List<string> ScreenOrder { get; set; } = new List<string>();
}
