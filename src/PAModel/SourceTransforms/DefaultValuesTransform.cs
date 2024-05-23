// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms;

internal class DefaultValuesTransform(
    Dictionary<string, ControlTemplate> templateStore,
    Theme theme,
    EditorStateStore stateStore)
{
    public void AfterRead(BlockNode node, bool inResponsiveContext)
    {
        var controlName = node.Name.Identifier;
        var templateName = node.Name.Kind.TypeName;
        var variantName = node.Name.Kind.OptionalVariant;

        var styleName = $"default{templateName.FirstCharToUpper()}Style";

        if (stateStore.TryGetControlState(controlName, out var controlState))
            styleName = controlState.StyleName;

        if (!templateStore.TryGetValue(templateName, out var template))
            template = null;

        var defaultHelper = new DefaultRuleHelper(styleName, template, templateName, variantName, theme, inResponsiveContext);
        foreach (var property in node.Properties.ToList())
        {
            var propName = property.Identifier;
            if (defaultHelper.TryGetDefaultRule(propName, out var defaultScript) && defaultScript == property.Expression.Expression)
                node.Properties.Remove(property);
        }
    }

    public void BeforeWrite(BlockNode node, bool inResponsiveContext)
    {
        var controlName = node.Name.Identifier;
        var templateName = node.Name.Kind.TypeName;
        var variantName = node.Name.Kind.OptionalVariant;

        var styleName = $"default{templateName.FirstCharToUpper()}Style";

        HashSet<string> propNames = null;
        if (stateStore.TryGetControlState(controlName, out var controlState) && controlState.Properties != null)
        {
            styleName = controlState.StyleName;
            propNames = new HashSet<string>(controlState.Properties.Select(state => state.PropertyName)
                .Concat(controlState.DynamicProperties?.Where(state => state.Property != null).Select(state => state.PropertyName) ?? []));
        }

        if (!templateStore.TryGetValue(templateName, out var template))
            template = null;

        var defaults = new DefaultRuleHelper(styleName, template, templateName, variantName, theme, inResponsiveContext).GetDefaultRules();
        foreach (var property in node.Properties)
        {
            defaults.Remove(property.Identifier);
        }

        foreach (var defaultkvp in defaults)
        {
            if (propNames != null && !propNames.Contains(defaultkvp.Key))
                continue;

            node.Properties.Add(new PropertyNode
            {
                Identifier = defaultkvp.Key,
                Expression = new ExpressionNode
                {
                    Expression = defaultkvp.Value
                }
            });
        }
    }
}
