// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.AppMagic.Authoring.Persistence;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal static class IRStateHelpers
    {
        internal static void SplitIRAndState(SourceFile file, EditorStateStore stateStore, TemplateStore templateStore, out BlockNode topParentIR)
        {
            var topParentJson = file.Value.TopParent;
            SplitIRAndState(topParentJson, topParentJson.Name, 0, stateStore, templateStore, out topParentIR);
        }

        private static void SplitIRAndState(ControlInfoJson.Item control, string topParentName, int index, EditorStateStore stateStore, TemplateStore templateStore, out BlockNode controlIR)
        {
            // Bottom up, recursively process children
            var children = new List<BlockNode>();
            var childIndex = 0;
            foreach (var child in control.Children)
            {
                SplitIRAndState(child, topParentName, childIndex, stateStore, templateStore, out var childBlock);
                children.Add(childBlock);
                ++childIndex;
            }

            var isComponentDef = control.Template.IsComponentDefinition ?? false;

            var customPropsToHide = new HashSet<string>();
            var functions = new List<FunctionNode>();
            if (control.Template.CustomProperties?.Any() ?? false)
            {
                if (!isComponentDef)
                {
                    // Skip component property params on instances
                    customPropsToHide = new HashSet<string>(control.Template.CustomProperties
                        .SelectMany(customProp =>
                            customProp.PropertyScopeKey.PropertyScopeRulesKey
                                .Select(propertyScopeRule => propertyScopeRule.Name)
                        ));
                }
                else
                {
                    // Create FunctionNodes on def
                    foreach (var customProp in control.Template.CustomProperties)
                    {
                        var name = customProp.Name;
                        customPropsToHide.Add(name);
                        var expression = control.Rules.First(rule => rule.Property == name).InvariantScript;
                        var expressionNode = new ExpressionNode() { Expression = expression };

                        var resultType = new TypeNode() { TemplateName = customProp.PropertyDataTypeKey };

                        var args = new List<TypedNameNode>();
                        foreach (var arg in customProp.PropertyScopeKey.PropertyScopeRulesKey)
                        {
                            args.Add(new TypedNameNode()
                            {
                                Identifier = arg.ScopeVariableInfo.ScopeVariableName,
                                Kind = new TypeNode()
                                {
                                    TemplateName = arg.ScopeVariableInfo.ScopePropertyDataType.ToString()
                                }
                            });
                        }

                        functions.Add(new FunctionNode()
                        {
                            Args = args,
                            ResultType = resultType,
                            Expression = expressionNode,
                            Identifier = name
                        });
                    }
                }
            }

            var properties = new List<PropertyNode>();
            var propStates = new List<PropertyState>();
            foreach (var property in control.Rules)
            {
                if (customPropsToHide.Contains(property.Property))
                    continue;
                var (prop, state) = SplitProperty(property);
                properties.Add(prop);
                propStates.Add(state);
            }

            controlIR = new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = control.Name,
                    Kind = new TypeNode()
                    {
                        TemplateName = control.Template.TemplateDisplayName ?? control.Template.Name,
                        OptionalVariant = string.IsNullOrEmpty(control.VariantName) ? null : control.VariantName
                    }
                },
                Children = children,
                Properties = properties,
                Functions = functions,
            };

            var controlState = new ControlState()
            {
                Name = control.Name,
                UniqueId = control.ControlUniqueId,
                TopParentName = topParentName,
                Properties = propStates,
                StyleName = control.StyleName,
                ExtensionData = control.ExtensionData,
                ParentIndex = index,
                IsComponentDefinition = isComponentDef,
            };

            // Set IsComponentDefintiion to false, just in case we're processing the def first and not the instance (avoids ordering affecting output)
            var templateState = new ControlInfoJson.Template(control.Template);
            templateState.ComponentDefinitionInfo = null;
            templateState.IsComponentDefinition = false;
            templateStore.AddTemplate(templateState);

            stateStore.TryAddControl(controlState);
        }

        private static (PropertyNode prop, PropertyState state) SplitProperty(ControlInfoJson.RuleEntry rule)
        {
            var script = rule.InvariantScript.Replace("\r\n", "\n").Replace("\r", "\n").TrimStart();
            var prop = new PropertyNode() { Expression = new ExpressionNode() { Expression = script }, Identifier = rule.Property };
            var state = new PropertyState() { PropertyName = rule.Property, ExtensionData = rule.ExtensionData, NameMap = rule.NameMap, RuleProviderType = rule.RuleProviderType };
            return (prop, state);
        }

        internal static SourceFile CombineIRAndState(BlockNode blockNode, EditorStateStore stateStore, TemplateStore templateStore)
        {
            var topParentJson = CombineIRAndState(blockNode, string.Empty, stateStore, templateStore);
            return SourceFile.New(new ControlInfoJson() { TopParent = topParentJson.item });
        }

        // Returns pair of item and index (with respect to parent order)
        private static (ControlInfoJson.Item item, int index) CombineIRAndState(BlockNode blockNode, string parent, EditorStateStore stateStore, TemplateStore templateStore)
        {
            var controlName = blockNode.Name.Identifier;

            // Bottom up, merge children first
            var children = new List<(ControlInfoJson.Item item, int index)>();
            foreach (var childBlock in blockNode.Children)
            {
                children.Add(CombineIRAndState(childBlock, controlName, stateStore, templateStore));
            }

            var orderedChildren = children.OrderBy(childPair => childPair.index).Select(pair => pair.item).ToArray();

            var templateIR = blockNode.Name.Kind;
            var templateName = templateIR.TemplateName;
            var variantName = templateIR.OptionalVariant;

            if (!templateStore.TryGetTemplate(templateName, out var template))
            {
                template = ControlInfoJson.Template.CreateDefaultTemplate(templateName, null);
            }

            ControlInfoJson.Item resultControlInfo;
            if (stateStore.TryGetControlState(controlName, out var state))
            {
                var properties = new List<ControlInfoJson.RuleEntry>();
                foreach (var propIR in blockNode.Properties)
                {
                    properties.Add(CombinePropertyIRAndState(propIR, state));
                }

                // Preserve ordering from serialized IR
                // Required for roundtrip checks
                properties = properties.OrderBy(prop => state.Properties.Select(propState => propState.PropertyName).ToList().IndexOf(prop.Property)).ToList();

                resultControlInfo = new ControlInfoJson.Item()
                {
                    Parent = parent,
                    Name = controlName,
                    ControlUniqueId = state.UniqueId,
                    VariantName = variantName ?? string.Empty,
                    Rules = properties.ToArray(),
                    StyleName = state.StyleName,
                    ExtensionData = state.ExtensionData,
                };

                if (state.IsComponentDefinition ?? false)
                {
                    template.ComponentDefinitionInfo = new ComponentDefinitionInfoJson(resultControlInfo, template.LastModifiedTimestamp, orderedChildren);
                    template = new ControlInfoJson.Template(template);
                    template.IsComponentDefinition = true;
                    template.ComponentDefinitionInfo = null;
                }
            }
            else
            {
                state = null;
                resultControlInfo = ControlInfoJson.Item.CreateDefaultControl();

                var properties = new List<ControlInfoJson.RuleEntry>();
                foreach (var propIR in blockNode.Properties)
                {
                    properties.Add(CombinePropertyIRAndState(propIR));
                }
                resultControlInfo.Rules = properties.ToArray();
            }
            resultControlInfo.Template = template;
            resultControlInfo.Children = orderedChildren;

            return (resultControlInfo, state?.ParentIndex ?? -1);
        }

        private static ControlInfoJson.RuleEntry CombinePropertyIRAndState(PropertyNode node, ControlState state = null)
        {
            var propName = node.Identifier;
            var expression = node.Expression.Expression;

            var property = new ControlInfoJson.RuleEntry();
            property.Property = propName;
            property.InvariantScript = expression;

            PropertyState propState = null;
            if (state?.Properties.ToDictionary(prop => prop.PropertyName).TryGetValue(propName, out propState) ?? false)
            {
                property.ExtensionData = propState.ExtensionData;
                property.NameMap = propState.NameMap;
                property.RuleProviderType = propState.RuleProviderType;
            }
            else
            {
                property.RuleProviderType = "Unknown";
            }

            return property;
        }
    }
}
