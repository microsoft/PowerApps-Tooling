using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates
{
    internal class ControlTemplateParser
    {
        internal static bool TryParseTemplate(string templateString, out ControlTemplate template)
        {
            template = null;
            try
            {
                var manifest = XDocument.Parse(templateString);
                if (manifest == null)
                    return false;


            }
            catch
            {
                return false;
            }
            return false;
        }
    }
}
