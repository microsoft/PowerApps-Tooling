using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState
{
    internal class TemplateStore
    {
        // Key is template name, case-sensitive
        public readonly Dictionary<string, CombinedTemplateState> Contents;

        public TemplateStore()
        {
            Contents = new Dictionary<string, CombinedTemplateState>();
        }

        public bool AddTemplate(CombinedTemplateState template)
        {
            var name = template.TemplateDisplayName ?? template.Name;
            if (Contents.ContainsKey(name))
                return false;

            Contents.Add(name, template);
            return true;
        }

        public bool TryGetTemplate(string templateName, out CombinedTemplateState template)
        {
            return Contents.TryGetValue(templateName, out template);
        }
    }
}
