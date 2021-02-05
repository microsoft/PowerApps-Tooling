// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        // The group control transform operates on the parent of the group control and so can't use
        // the existing template transform mechanism
        // Refactor if another transform is added that also operates on a control based on it's children
        internal GroupControlTransform _groupControlTransform;

        internal DefaultValuesTransform _defaultValTransform;

        public SourceTransformer(ErrorContainer errors, Dictionary<string, ControlTemplate> defaultValueTemplates, Theme theme, ComponentInstanceTransform componentInstanceTransform,
            EditorStateStore stateStore, TemplateStore templateStore, Entropy entropy)
        {
            _templateTransforms = new List<IControlTemplateTransform>();
            _templateTransforms.Add(new GalleryTemplateTransform(defaultValueTemplates, stateStore));
            _templateTransforms.Add(new AppTestTransform(errors, templateStore, stateStore, entropy));
            _templateTransforms.Add(componentInstanceTransform);

            _groupControlTransform = new GroupControlTransform(errors, stateStore, entropy);

            _defaultValTransform = new DefaultValuesTransform(defaultValueTemplates, theme, stateStore);            
        }

        public void ApplyAfterRead(BlockNode control, bool inResponsiveContext = false)
        {
            var controlTemplateName = control.Name?.Kind?.TypeName ?? string.Empty;

            var childResponsiveContext = DynamicProperties.AddsChildDynamicProperties(controlTemplateName);
            foreach (var child in control.Children)
            {
                ApplyAfterRead(child, childResponsiveContext);
            }

            // Apply default values first, before re-arranging controls
            _defaultValTransform.AfterRead(control, inResponsiveContext);

            foreach (var transform in _templateTransforms)
            {
                if (transform.TargetTemplates.Contains(controlTemplateName))
                    transform.AfterRead(control);
            }

            _groupControlTransform.AfterRead(control);
        }
        public void ApplyBeforeWrite(BlockNode control, bool inResponsiveContext = false)
        {
            var controlTemplateName = control.Name?.Kind?.TypeName ?? string.Empty;
            var childResponsiveContext = DynamicProperties.AddsChildDynamicProperties(controlTemplateName);

            _groupControlTransform.BeforeWrite(control);
            foreach (var transform in _templateTransforms.Reverse())
            {
                if (transform.TargetTemplates.Contains(controlTemplateName))
                    transform.BeforeWrite(control);
            }

            foreach (var child in control.Children)
            {
                ApplyBeforeWrite(child, childResponsiveContext);
            }

            // Apply default values last, after controls are back to msapp shape
            _defaultValTransform.BeforeWrite(control, inResponsiveContext);
        }
    }
}
