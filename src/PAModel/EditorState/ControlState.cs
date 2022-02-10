using System.Collections.Generic;
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

        [JsonIgnore]
        public string TopParentName { get; set; }

        private bool _isTopParent;
        // Should only return true or null. Returning null will prevent the field from being serialized.
        public bool? IsTopParent
        {
            get
            {
                if (_isTopParent || Name.Equals(TopParentName))
                    return true;

                return null;
            }
            set
            {
                _isTopParent = value ?? false;
            }
        }

        // These are properties with namemaps/info beyond the ones present in the control template
        // Key is property name
        public List<PropertyState> Properties { get; set; }

        // These are properties specific to AutoLayout controls
        public List<DynamicPropertyState> DynamicProperties { get; set; }

        // Has to be here for back compat unfortunately
        // Newer apps write false here, but we need to roundtrip older apps where it was missing
        // even though this can be re-derived from the existence of DynamicProperties
        public bool? HasDynamicProperties { get; set; }

        public bool? AllowAccessToGlobals { get; set; }

        // Doesn't get written to .msapp
        // Represents the index at which this property appears in it's parent's children list
        public int ParentIndex { get; set; } = -1;

        // For matching up within a Theme.
        public string StyleName { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }

        // Not sure if there's a better way of representing this
        // For galleries, we need to persist the galleryTemplate control name as a child of this
        // to properly pair up the studio state for roundtripping
        // This isn't needed otherwise, if we weren't worried about exact round-tripping we could recreate the control with a different name
        public string GalleryTemplateChildName { get; set; } = null;

        public bool? IsComponentDefinition { get; set; }

        public bool IsGroupControl { get; set; }

        // This is a list of the controls represented as a child of the group control in studio
        // Used in GroupControlTransform.cs, and not written to .editorstate.json
        internal List<string> GroupedControlsKey;

        public ControlState Clone()
        {
            var newState = Utilities.JsonClone(this);
            newState.TopParentName = TopParentName;
            return newState;
        }
    }
}
