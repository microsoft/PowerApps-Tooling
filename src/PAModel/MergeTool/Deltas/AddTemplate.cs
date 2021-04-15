using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas
{
    internal class AddTemplate : IDelta
    {
        public string Name;
        public CombinedTemplateState Template;
        public TemplatesJson.TemplateJson JsonTemplate;

        public void Apply(CanvasDocument document)
        {
            if (document._templateStore.AddTemplate(Name, Template) && JsonTemplate != null)
            {
                document._templates.UsedTemplates = document._templates.UsedTemplates.Concat(new[] { JsonTemplate }).ToArray();
            }
        }
    }
}
