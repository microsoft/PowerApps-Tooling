// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Manifest combines the various property/header/publish files into one. 
    class CanvasManifestJson
    {
        // Format version. 
        // this is most critical for verisoning. 
        public Version FormatVersion { get; set; }

        // Issue#21: Server Side changes should address noise in Properties.json
        public DocumentPropertiesJson Properties { get; set; }

        // SavedDate
        public HeaderJson Header { get; set; }

        // Logo file
        // $$$ Other files?
        public PublishInfoJson PublishInfo { get; set; }
    }
}
