// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Extensions;

namespace Microsoft.PowerPlatform.Formulas.Tools.EditorState;

internal class TemplateStore
{
    // Key is template name, case-sensitive
    public readonly Dictionary<string, CombinedTemplateState> Contents;

    public TemplateStore()
    {
        Contents = new Dictionary<string, CombinedTemplateState>();
    }

    public TemplateStore(TemplateStore other)
    {
        Contents = other.Contents.JsonClone();
    }

    public bool AddTemplate(string name, CombinedTemplateState template)
    {
        if (Contents.ContainsKey(name))
            return false;

        Contents.Add(name, template);
        return true;
    }

    public bool TryGetTemplate(string templateName, out CombinedTemplateState template)
    {
        return Contents.TryGetValue(templateName, out template);
    }

    // This renames a template
    // It should only be called after the templates are loaded
    // And calls must be symmetrical between read/write
    public bool TryRenameTemplate(string oldTemplateName, string newTemplateName)
    {
        if (!Contents.TryGetValue(oldTemplateName, out var template))
            return false;
        if (Contents.ContainsKey(newTemplateName))
            return false;
        Contents.Remove(oldTemplateName);
        Contents.Add(newTemplateName, template);
        return true;
    }
}
