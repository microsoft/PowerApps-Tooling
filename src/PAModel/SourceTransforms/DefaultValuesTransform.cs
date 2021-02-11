// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        public void AfterRead(BlockNode node, bool inResponsiveContext)
        {
            var controlName = node.Name.Identifier;
            var templateName = node.Name.Kind.TypeName;

            var styleName = $"default{templateName.FirstCharToUpper()}Style";

            if (_controlStore.TryGetControlState(controlName, out var controlState))
                styleName = controlState.StyleName;

            ControlTemplate template;
            if (!_templateStore.TryGetValue(templateName, out template))
                template = null;

            var defaultHelper = new DefaultRuleHelper(styleName, template, templateName, _theme, inResponsiveContext);
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

            var styleName = $"default{templateName.FirstCharToUpper()}Style";

            HashSet<string> propNames = null;
            if (_controlStore.TryGetControlState(controlName, out var controlState))
            {
                styleName = controlState.StyleName;
                propNames = new HashSet<string>(controlState.Properties.Select(state => state.PropertyName)
                    .Concat(controlState.DynamicProperties?.Select(state => state.PropertyName) ?? Enumerable.Empty<string>()));
            }

            ControlTemplate template;
            if (!_templateStore.TryGetValue(templateName, out template))
                template = null;

            var defaults = new DefaultRuleHelper(styleName, template, templateName, _theme, inResponsiveContext).GetDefaultRules();
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
}
