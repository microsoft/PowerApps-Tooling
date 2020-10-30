using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.SourceTransforms
{
    internal class DefaultValuesTransform
    {
        private EditorStateStore _controlStore;
        private Theme _theme;
        private Dictionary<string, ControlTemplate> _templateStore;

        public DefaultValuesTransform(Dictionary<string, ControlTemplate> templateStore, Theme theme, EditorStateStore stateStore)
        {
            _controlStore = stateStore;
            _templateStore = templateStore;
            _theme = theme;
        }

        public void AfterRead(BlockNode node)
        {
            var controlName = node.Name.Identifier;
            var templateName = node.Name.Kind.TemplateName;

            var styleName = $"default{templateName}Style";

            if (_controlStore.TryGetControlState(controlName, out var controlState))
                styleName = controlState.StyleName;

            ControlTemplate template;
            if (!_templateStore.TryGetValue(templateName, out template))
                template = null;

            var defaultHelper = new DefaultRuleHelper(styleName, template, _theme);
            foreach (var property in node.Properties.ToList())
            {
                var propName = property.Identifier;
                if (defaultHelper.TryGetDefaultRule(propName, out var defaultScript) && defaultScript == property.Expression.Expression)
                    node.Properties.Remove(property);
            }
        }

        public void BeforeWrite(BlockNode node)
        {
            var controlName = node.Name.Identifier;
            var templateName = node.Name.Kind.TemplateName;

            var styleName = $"default{templateName}Style";

            if (_controlStore.TryGetControlState(controlName, out var controlState))
                styleName = controlState.StyleName;

            ControlTemplate template;
            if (!_templateStore.TryGetValue(templateName, out template))
                template = null;

            var defaults = new DefaultRuleHelper(styleName, template, _theme).GetDefaultRules();
            foreach (var property in node.Properties)
            {
                defaults.Remove(property.Identifier);               
            }

            foreach (var defaultkvp in defaults)
            {
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
}
