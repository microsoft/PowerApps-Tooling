using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    internal class TemplateStore
    {
        private Dictionary<string, ControlInfoJson.Template> _templates;
        public TemplateStore()
        {
            _templates = new Dictionary<string, ControlInfoJson.Template>();
        }

        public bool AddTemplate(ControlInfoJson.Template template)
        {
            if (_templates.ContainsKey(template.Name))
                return false;

            _templates.Add(template.Name, template);
            return true;
        }

        public bool TryGetTemplate(string templateName, out ControlInfoJson.Template template)
        {
            return _templates.TryGetValue(templateName, out template);
        }
    }
}
