using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    internal class ComponentDefinitionTransform
    {
        private TemplateStore _templateStore;
        private ComponentInstanceTransform _componentInstanceTransform;

        public ComponentDefinitionTransform(TemplateStore templateStore, ComponentInstanceTransform componentInstanceTransform)
        {
            _templateStore = templateStore;
            _componentInstanceTransform = componentInstanceTransform;
        }

        public void AfterRead(BlockNode control)
        {
            var templateName = control.Name?.Kind?.TypeName ?? string.Empty;
            if (!_templateStore.TryGetTemplate(templateName, out var componentTemplate) ||
                !(componentTemplate.IsComponentTemplate ?? false))
            {
                return;
            }

            var kindName = componentTemplate.ComponentType.ToString();
            // Older apps don't have component type set, fall back to using the Id
            if (!componentTemplate.ComponentType.HasValue)
            {
                if (componentTemplate.Id == ControlInfoJson.Template.UxComponentId)
                    kindName = ComponentType.CanvasComponent.ToString();
                else if (componentTemplate.Id == ControlInfoJson.Template.DataComponentId)
                    kindName = ComponentType.DataComponent.ToString();
                else
                    return; // We couldn't find a component type. Just keep using the guid.
            }

            var controlName = control.Name.Identifier;
            if (!_templateStore.TryRenameTemplate(templateName, controlName))
                return;

            _componentInstanceTransform.ComponentRenames.Add(templateName, controlName);
            control.Name.Kind.TypeName = kindName;
        }                  


        public void BeforeWrite(BlockNode control)
        {
            var controlName = control.Name.Identifier;
            var templateName = control.Name?.Kind?.TypeName ?? string.Empty;

            if (!(templateName == ComponentType.CanvasComponent.ToString() ||
                templateName == ComponentType.DataComponent.ToString() ||
                templateName == ComponentType.FunctionComponent.ToString()))
            {
                return;
            }

            if (!_templateStore.TryGetTemplate(controlName, out var componentTemplate) ||
                !(componentTemplate.IsComponentTemplate ?? false))
            {
                throw new InvalidOperationException($"Unable to find template for component {controlName}");
            }

            var originalTemplateName = componentTemplate.Name;
            if (!_templateStore.TryRenameTemplate(controlName, originalTemplateName))
            {
                throw new InvalidOperationException($"Unable to update template for component {controlName}, id {originalTemplateName}");
            }

            _componentInstanceTransform.ComponentRenames.Add(controlName, originalTemplateName);
            control.Name.Kind.TypeName = originalTemplateName;
        }
    }
}
