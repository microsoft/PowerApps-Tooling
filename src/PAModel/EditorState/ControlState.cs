using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    /// <summary>
    /// Per control, this is the studio state content that doesn't wind up in the IR
    /// Similar to <seealso cref="ControlInfoJson.Item"/> without the info encoded by .pa
    /// </summary>
    internal class ControlState
    {
        public string Name { get; set; }
        public string UniqueId { get; set; }

        [JsonIgnore]
        public string TopParentName { get; set; }

        // These are properties with namemaps/info beyond the ones present in the control template
        // Key is property name
        public Dictionary<string, PropertyState> Properties { get; set; }
        public int PublishOrderIndex { get; set; }

        // For matching up within a Theme.
        public string StyleName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

        // Not sure if there's a better way of representing this
        // For galleries, we need to persist the galleryTemplate control name as a child of this
        // to properly pair up the studio state for roundtripping
        // This isn't needed otherwise, if we weren't worried about exact round-tripping we could recreate the control with a different name
        public string GalleryTemplateChildName { get; set; } = null;
    }
}
