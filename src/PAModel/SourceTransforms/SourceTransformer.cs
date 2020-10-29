using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    internal class SourceTransformer
    {
        internal IList<IControlTemplateTransform> _templateTransforms;

        // Control store and theme defaults to null temporarily
        // Not needed for gallery write, implement proper controlstore soon
        public SourceTransformer(Dictionary<string, ControlTemplate> templateStore, EditorStateStore stateStore)
        {
            _templateTransforms = new List<IControlTemplateTransform>();
            _templateTransforms.Add(new GalleryTemplateTransform(templateStore, stateStore));
        }

        public void ApplyBeforeWrite(BlockNode control)
        {
            foreach (var child in control.Children)
            {
                ApplyBeforeWrite(child);
            }

            var controlTemplateName = control.Name?.Kind?.TemplateName ?? string.Empty;

            foreach (var transform in _templateTransforms)
            {
                if (controlTemplateName == transform.TargetTemplate)
                    transform.BeforeWrite(control);
            }
        }
        public void ApplyAfterParse(BlockNode control)
        {
            foreach (var child in control.Children)
            {
                ApplyBeforeWrite(child);
            }

            var controlTemplateName = control.Name?.Kind?.TemplateName ?? string.Empty;

            foreach (var transform in _templateTransforms)
            {
                if (controlTemplateName == transform.TargetTemplate)
                    transform.BeforeWrite(control);
            }
        }
    }
}
