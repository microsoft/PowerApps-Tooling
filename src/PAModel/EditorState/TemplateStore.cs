using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    internal class TemplateStore
    {
        public Dictionary<string, ControlInfoJson.Template> Contents { get; private set; }

        public TemplateStore()
        {
            Contents = new Dictionary<string, ControlInfoJson.Template>();
        }

        public bool AddTemplate(ControlInfoJson.Template template)
        {
            if (Contents.ContainsKey(template.Name))
                return false;

            Contents.Add(template.Name, template);
            return true;
        }

        public bool TryGetTemplate(string templateName, out ControlInfoJson.Template template)
        {
            return Contents.TryGetValue(templateName, out template);
        }
    }
}
