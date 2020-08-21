using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    // Manifest combines the various property/header/publish files into one. 
    class CanvasManifestJson
    {
        // $$$ Contains lots of noisy data
        public DocumentPropertiesJson Properties { get; set; }

        // SavedDate
        public HeaderJson Header { get; set; }

        // Logo file
        // $$$ Other files?
        public PublishInfoJson PublishInfo { get; set; }
    }
}
