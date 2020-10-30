using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    internal class SourceTransformer
    {
        internal IList<IControlTemplateTransform> _templateTransforms;
        internal DefaultValuesTransform _defaultValTransform;

        public SourceTransformer(Dictionary<string, ControlTemplate> templateStore, Theme theme, EditorStateStore stateStore)
        {
            _templateTransforms = new List<IControlTemplateTransform>();
            _templateTransforms.Add(new GalleryTemplateTransform(templateStore, stateStore));

            _defaultValTransform = new DefaultValuesTransform(templateStore, theme, stateStore);            
        }

        public void ApplyAfterRead(BlockNode control)
        {
            foreach (var child in control.Children)
            {
                ApplyAfterRead(child);
            }

            // Apply default values first, before re-arranging controls
            _defaultValTransform.AfterRead(control);

            var controlTemplateName = control.Name?.Kind?.TemplateName ?? string.Empty;

            foreach (var transform in _templateTransforms)
            {
                if (controlTemplateName == transform.TargetTemplate)
                    transform.AfterRead(control);
            }
        }
        public void ApplyBeforeWrite(BlockNode control)
        {
            var controlTemplateName = control.Name?.Kind?.TemplateName ?? string.Empty;

            foreach (var transform in _templateTransforms.Reverse())
            {
                if (controlTemplateName == transform.TargetTemplate)
                    transform.BeforeWrite(control);
            }

            foreach (var child in control.Children)
            {
                ApplyBeforeWrite(child);
            }

            // Apply default values last, after controls are back to msapp shape
            _defaultValTransform.BeforeWrite(control);
        }
    }
}
