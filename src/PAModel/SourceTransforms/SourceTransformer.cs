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

        // $$$ Pass ErrorContainer to transforms and replace exception based error handling
        public SourceTransformer(ErrorContainer errors, Dictionary<string, ControlTemplate> defaultValueTemplates, Theme theme, ComponentInstanceTransform componentInstanceTransform,
            EditorStateStore stateStore, TemplateStore templateStore)
        {
            _templateTransforms = new List<IControlTemplateTransform>();
            _templateTransforms.Add(new GalleryTemplateTransform(defaultValueTemplates, stateStore));
            _templateTransforms.Add(new AppTestTransform(errors, templateStore, stateStore));
            _templateTransforms.Add(componentInstanceTransform);

            _defaultValTransform = new DefaultValuesTransform(defaultValueTemplates, theme, stateStore);            
        }

        public void ApplyAfterRead(BlockNode control)
        {
            foreach (var child in control.Children)
            {
                ApplyAfterRead(child);
            }

            // Apply default values first, before re-arranging controls
            _defaultValTransform.AfterRead(control);

            var controlTemplateName = control.Name?.Kind?.TypeName ?? string.Empty;

            foreach (var transform in _templateTransforms)
            {
                if (transform.TargetTemplates.Contains(controlTemplateName))
                    transform.AfterRead(control);
            }
        }
        public void ApplyBeforeWrite(BlockNode control)
        {
            var controlTemplateName = control.Name?.Kind?.TypeName ?? string.Empty;

            foreach (var transform in _templateTransforms.Reverse())
            {
                if (transform.TargetTemplates.Contains(controlTemplateName))
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
