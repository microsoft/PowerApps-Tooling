using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AppMagic.Authoring.Persistence
{
    // Manifest combines the various property/header/publish files into one. 
    class CanvasManifestJson
    {
        // Format version. 
        // this is most critical for verisoning. 
        public Version FormatVersion { get; set; }

        // $$$ Contains lots of noisy data
        public DocumentPropertiesJson Properties { get; set; }

        // SavedDate
        public HeaderJson Header { get; set; }

        // Logo file
        // $$$ Other files?
        public PublishInfoJson PublishInfo { get; set; }
    }
}
