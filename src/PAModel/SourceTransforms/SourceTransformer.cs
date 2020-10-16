using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    internal class SourceTransformer
    {
        internal IList<IControlTemplateTransform> _templateTransforms;

        // Control store and theme defaults to null temporarily
        // not needed for gallery write, implement proper controlstore soon
        public SourceTransformer(Dictionary<string, ControlTemplate> templateStore, Theme theme, Dictionary<string, ControlInfoJson.Item> controlStore = null)
        {
            _templateTransforms = new List<IControlTemplateTransform>();
            _templateTransforms.Add(new GalleryTemplateTransform(templateStore, theme, controlStore));
        }

        public void ApplyBeforeWrite(ControlInfoJson topFile)
        {
            ApplyBeforeWrite(topFile.TopParent);
        }
        public void ApplyAfterParse(ControlInfoJson topFile)
        {
            ApplyAfterParse(topFile.TopParent);
        }

        // Bottom-up apply transforms
        // This really should be operating on some kind of IR not ControlInfoJson directly
        private void ApplyBeforeWrite(ControlInfoJson.Item control)
        {
            foreach (var child in control.Children)
            {
                ApplyBeforeWrite(child);
            }

            foreach (var transform in _templateTransforms)
            {
                if (control.Template.Name == transform.TargetTemplate)
                    transform.BeforeWrite(control);
            }
        }

        private void ApplyAfterParse(ControlInfoJson.Item control)
        {
            foreach (var child in control.Children)
            {
                ApplyAfterParse(child);
            }

            foreach (var transform in _templateTransforms)
            {
                if (control.Template.Name == transform.TargetTemplate)
                    transform.AfterParse(control);
            }
        }
    }
}
