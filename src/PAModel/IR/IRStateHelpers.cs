// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.AppMagic.Authoring.Persistence;
using System;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal static class IRStateHelpers
    {
        internal static void SplitIRAndState(SourceFile file, EditorStateStore stateStore, TemplateStore templateStore, Entropy entropy, out BlockNode topParentIR)
        {
            var topParentJson = file.Value.TopParent;
            SplitIRAndState(topParentJson, topParentJson.Name, 0, stateStore, templateStore, entropy, out topParentIR);
        }

        private static void SplitIRAndState(ControlInfoJson.Item control, string topParentName, int index, EditorStateStore stateStore, TemplateStore templateStore, Entropy entropy, out BlockNode controlIR)
        {
            // Bottom up, recursively process children
            var children = new List<BlockNode>();
            var childIndex = 0;
            foreach (var child in control.Children)
            {
                SplitIRAndState(child, topParentName, childIndex, stateStore, templateStore, entropy, out var childBlock);
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
                        .Where(customProp => customProp.IsFunctionProperty)
                        .SelectMany(customProp =>
                            customProp.PropertyScopeKey.PropertyScopeRulesKey
                                .Select(propertyScopeRule => propertyScopeRule.Name)
                        ));
                }
                else
                {
                    // Create FunctionNodes on def
                    foreach (var customProp in control.Template.CustomProperties.Where(prop => prop.IsFunctionProperty))
                    {
                        var name = customProp.Name;
                        customPropsToHide.Add(name);
                        var rule = control.Rules.FirstOrDefault(rule => rule.Property == name);
                        if (rule == null)
                        {
                            // Control does not have a rule for the custom property. 
                            continue;
                        }
                        var expression = rule.InvariantScript;
                        var expressionNode = new ExpressionNode() { Expression = expression };

                        var resultType = new TypeNode() { TypeName = customProp.PropertyDataTypeKey };

                        var args = new List<TypedNameNode>();
                        var argMetadata = new List<ArgMetadataBlockNode>();
                        foreach (var arg in customProp.PropertyScopeKey.PropertyScopeRulesKey)
                        {
                            customPropsToHide.Add(arg.Name);
                            args.Add(new TypedNameNode()
                            {
                                Identifier = arg.ScopeVariableInfo.ScopeVariableName,
                                Kind = new TypeNode()
                                {
                                    TypeName = ((PropertyDataType)arg.ScopeVariableInfo.ScopePropertyDataType).ToString()
                                }
                            });

                            var invariantScript = control.Rules.First(rule => rule.Property == arg.Name)?.InvariantScript;
                            argMetadata.Add(new ArgMetadataBlockNode()
                            {
                                Identifier = arg.ScopeVariableInfo.ScopeVariableName,
                                Default = new ExpressionNode()
                                {
                                    Expression = invariantScript ?? arg.ScopeVariableInfo.DefaultRule
                                },
                            });

                            // Handle the case where invariantScript value of the property is not same as the default script.
                            if (invariantScript != null && invariantScript != arg.ScopeVariableInfo.DefaultRule)
                            {
                                entropy.FunctionParamsInvariantScripts.Add(arg.Name, new string[] { arg.ScopeVariableInfo.DefaultRule, invariantScript });
                            }

                            arg.ScopeVariableInfo.DefaultRule = null;
                            arg.ScopeVariableInfo.ScopePropertyDataType = null;
                            arg.ScopeVariableInfo.ParameterIndex = null;
                            arg.ScopeVariableInfo.ParentPropertyName = null;
                        }

                        argMetadata.Add(new ArgMetadataBlockNode()
                        {
                            Identifier = PAConstants.ThisPropertyIdentifier,
                            Default = new ExpressionNode()
                            {
                                Expression = expression,
                            },
                        });

                        functions.Add(new FunctionNode()
                        {
                            Args = args,
                            Metadata = argMetadata,
                            Identifier = name
                        });
                    }
                }
            }

            var properties = new List<PropertyNode>();
            var propStates = new List<PropertyState>();
            var dynPropStates = new List<DynamicPropertyState>();
            foreach (var property in control.Rules)
            {
                var (prop, state) = SplitProperty(property);
                propStates.Add(state);

                if (customPropsToHide.Contains(property.Property))
                    continue;

                properties.Add(prop);
            }

            foreach (var property in control.DynamicProperties ?? Enumerable.Empty<ControlInfoJson.DynamicPropertyJson>())
            {
                if (property.Rule == null)
                {
                    dynPropStates.Add(new DynamicPropertyState() { PropertyName = property.PropertyName });
                    continue;
                }

                var (prop, state) = SplitDynamicProperty(property);
                dynPropStates.Add(state);
                properties.Add(prop);
            }

            controlIR = new BlockNode()
            {
                Name = new TypedNameNode()
                {
                    Identifier = control.Name,
                    Kind = new TypeNode()
                    {
                        TypeName = control.Template.TemplateDisplayName ?? control.Template.Name,
                        OptionalVariant = string.IsNullOrEmpty(control.VariantName) ? null : control.VariantName
                    }
                },
                Children = children.ToList(),
                Properties = properties,
                Functions = functions,
            };


            if (templateStore.TryGetTemplate(control.Template.Name, out var templateState))
            {
                if (isComponentDef)
                {
                    templateState.IsComponentTemplate = true;
                    templateState.CustomProperties = control.Template.CustomProperties;
                }
            }
            else
            {
                templateState = new CombinedTemplateState(control.Template);
                templateState.ComponentDefinitionInfo = null;
                var templateName = templateState.TemplateDisplayName ?? templateState.Name;
                templateStore.AddTemplate(templateName, templateState);
            }

            SplitCustomTemplates(entropy, control.Template, control.Name);

            entropy.ControlUniqueIds.Add(control.Name, int.Parse(control.ControlUniqueId));
            var controlState = new ControlState()
            {
                Name = control.Name,
                TopParentName = topParentName,
                Properties = propStates,
                DynamicProperties = dynPropStates.Any() ? dynPropStates : null,
                HasDynamicProperties = control.HasDynamicProperties,
                StyleName = control.StyleName,
                IsGroupControl = control.IsGroupControl,
                GroupedControlsKey = control.GroupedControlsKey,
                ExtensionData = control.ExtensionData,
                ParentIndex = index,
                AllowAccessToGlobals = control.AllowAccessToGlobals,
                IsComponentDefinition = control.Template.IsComponentDefinition,
            };

            stateStore.TryAddControl(controlState);
        }

        private static (PropertyNode prop, PropertyState state) SplitProperty(ControlInfoJson.RuleEntry rule)
        {
            var script = rule.InvariantScript;
            var prop = new PropertyNode() { Expression = new ExpressionNode() { Expression = script }, Identifier = rule.Property };
            var state = new PropertyState() { PropertyName = rule.Property, ExtensionData = rule.ExtensionData, NameMap = rule.NameMap, RuleProviderType = rule.RuleProviderType };
            return (prop, state);
        }

        private static (PropertyNode prop, DynamicPropertyState state) SplitDynamicProperty(ControlInfoJson.DynamicPropertyJson dynamicProperty)
        {
            var (prop, propertyState) = SplitProperty(dynamicProperty.Rule);
            var state = new DynamicPropertyState() { PropertyName = propertyState.PropertyName, Property = propertyState, ExtensionData = dynamicProperty.ExtensionData };
            return (prop, state);
        }

        internal static SourceFile CombineIRAndState(BlockNode blockNode, ErrorContainer errors, EditorStateStore stateStore, TemplateStore templateStore, UniqueIdRestorer uniqueIdRestorer, Entropy entropy)
        {
            var topParentJson = CombineIRAndState(blockNode, errors, string.Empty, false, stateStore, templateStore, uniqueIdRestorer, entropy);
            return SourceFile.New(new ControlInfoJson() { TopParent = topParentJson.item });
        }

        // Returns pair of item and index (with respect to parent order)
        private static (ControlInfoJson.Item item, int index) CombineIRAndState(BlockNode blockNode, ErrorContainer errors, string parent, bool isInResponsiveLayout, EditorStateStore stateStore, TemplateStore templateStore, UniqueIdRestorer uniqueIdRestorer, Entropy entropy)
        {
            var controlName = blockNode.Name.Identifier;
            var templateIR = blockNode.Name.Kind;
            var templateName = templateIR.TypeName;
            var variantName = templateIR.OptionalVariant;

            // Bottom up, merge children first
            var children = new List<(ControlInfoJson.Item item, int index)>();
            foreach (var childBlock in blockNode.Children)
            {
                children.Add(CombineIRAndState(childBlock, errors, controlName, DynamicProperties.AddsChildDynamicProperties(templateName, variantName), stateStore, templateStore, uniqueIdRestorer, entropy));
            }

            var orderedChildren = children.OrderBy(childPair => childPair.index).Select(pair => pair.item).ToArray();

            ControlInfoJson.Template template;
            if (!templateStore.TryGetTemplate(templateName, out var templateState))
            {
                template = ControlInfoJson.Template.CreateDefaultTemplate(templateName, null);
            }
            else
            {
                template = templateState.ToControlInfoTemplate();
            }

            RecombineCustomTemplates(entropy, template, controlName);

            var uniqueId = uniqueIdRestorer.GetControlId(controlName);
            ControlInfoJson.Item resultControlInfo;
            if (stateStore.TryGetControlState(controlName, out var state))
            {
                var properties = new List<ControlInfoJson.RuleEntry>();
                var dynamicProperties = new List<ControlInfoJson.DynamicPropertyJson>();
                foreach (var propIR in blockNode.Properties)
                {
                    // Dynamic properties could be null for the galleryTemplateTemplate 
                    if (isInResponsiveLayout && state.DynamicProperties != null && DynamicProperties.IsResponsiveLayoutProperty(propIR.Identifier))
                    {
                        dynamicProperties.Add(CombineDynamicPropertyIRAndState(propIR, state));
                    }
                    else
                    {
                        properties.Add(CombinePropertyIRAndState(propIR, errors, state));
                    }
                }

                if (isInResponsiveLayout && state.DynamicProperties != null)
                {
                    // Add dummy dynamic output props in the state at the end
                    foreach (var dynPropState in state.DynamicProperties.Where(propState => propState.Property == null))
                    {
                        dynamicProperties.Add(new ControlInfoJson.DynamicPropertyJson() { PropertyName = dynPropState.PropertyName });
                    }

                    // Reorder to preserve roundtripping
                    dynamicProperties = dynamicProperties.OrderBy(prop => state.DynamicProperties.Select(propState => propState.PropertyName).ToList().IndexOf(prop.PropertyName)).ToList();
                }

                if (blockNode.Functions.Any())
                {
                    foreach (var func in blockNode.Functions)
                    {
                        var funcName = func.Identifier;
                        var thisPropertyBlock = func.Metadata.FirstOrDefault(metadata => metadata.Identifier == PAConstants.ThisPropertyIdentifier);
                        if (thisPropertyBlock == default)
                        {
                            errors.ParseError(func.SourceSpan.GetValueOrDefault(), "Function definition missing ThisProperty block");
                            throw new DocumentException();
                        }
                        properties.Add(GetPropertyEntry(state, errors, funcName, thisPropertyBlock.Default.Expression));

                        foreach (var arg in func.Metadata)
                        {
                            if (arg.Identifier == PAConstants.ThisPropertyIdentifier)
                                continue;

                            var propName = funcName + "_" + arg.Identifier;
                            properties.Add(GetPropertyEntry(state, errors, propName, entropy.GetInvariantScript(propName, arg.Default.Expression)));
                        }

                        RepopulateTemplateCustomProperties(func, templateState, errors, entropy);
                    }
                }
                else if (template.CustomProperties?.Any(prop => prop.IsFunctionProperty) ?? false)
                {
                    // For component uses, recreate the dummy props for function parameters
                    foreach (var hiddenScopeRule in template.CustomProperties.Where(prop => prop.IsFunctionProperty).SelectMany(prop => prop.PropertyScopeKey.PropertyScopeRulesKey))
                    {
                        if (!properties.Any(x => x.Property == hiddenScopeRule.Name))
                        {
                            var script = entropy.GetInvariantScript(hiddenScopeRule.Name, hiddenScopeRule.ScopeVariableInfo.DefaultRule);
                            properties.Add(GetPropertyEntry(state, errors, hiddenScopeRule.Name, script));
                        }
                    }
                }

                // Preserve ordering from serialized IR
                // Required for roundtrip checks
                properties = properties.OrderBy(prop => state.Properties?.Select(propState => propState.PropertyName).ToList().IndexOf(prop.Property) ?? -1).ToList();
                resultControlInfo = new ControlInfoJson.Item()
                {
                    Parent = parent,
                    Name = controlName,
                    ControlUniqueId = uniqueId.ToString(),
                    VariantName = variantName ?? string.Empty,
                    Rules = properties.ToArray(),
                    DynamicProperties = (isInResponsiveLayout && dynamicProperties.Any()) ? dynamicProperties.ToArray() : null,
                    HasDynamicProperties = state.HasDynamicProperties,
                    StyleName = state.StyleName,
                    ExtensionData = state.ExtensionData,
                    IsGroupControl = state.IsGroupControl,
                    GroupedControlsKey = state.GroupedControlsKey,
                    AllowAccessToGlobals = (state.IsComponentDefinition ?? false) ? templateState?.ComponentManifest?.AllowAccessToGlobals : state.AllowAccessToGlobals,
                };

                if (state.IsComponentDefinition ?? false)
                {
                    // Before AllowAccessToGlobals added to ComponentDefinition in msapp, it is present in component manifest as well.
                    // So when reconstructing componentdefinition, we need to identify if it was ever present on component definition or not.
                    // For this, we use state IsAllowAccessToGlobalsPresent.
                    templateState.ComponentDefinitionInfo = new ComponentDefinitionInfoJson(resultControlInfo, template.LastModifiedTimestamp, orderedChildren, entropy.IsLegacyComponentAllowGlobalScopeCase ? null : templateState.ComponentManifest.AllowAccessToGlobals);
                    template = templateState.ToControlInfoTemplate();
                    template.IsComponentDefinition = true;

                    // Set it null so that it can be ignored. We have this information at other place.
                    template.ComponentDefinitionInfo = null;
                }
                else
                {
                    template.IsComponentDefinition = state.IsComponentDefinition;
                }
            }
            else
            {
                state = null;
                resultControlInfo = ControlInfoJson.Item.CreateDefaultControl();
                resultControlInfo.Name = controlName;
                resultControlInfo.ControlUniqueId = uniqueId.ToString();
                resultControlInfo.Parent = parent;
                resultControlInfo.VariantName = variantName ?? string.Empty;
                resultControlInfo.StyleName = $"default{templateName.FirstCharToUpper()}Style";
                var properties = new List<ControlInfoJson.RuleEntry>();
                var dynamicProperties = new List<ControlInfoJson.DynamicPropertyJson>();
                foreach (var propIR in blockNode.Properties)
                {
                    if (isInResponsiveLayout && DynamicProperties.IsResponsiveLayoutProperty(propIR.Identifier))
                    {
                        dynamicProperties.Add(CombineDynamicPropertyIRAndState(propIR));
                    }
                    else
                    {
                        properties.Add(CombinePropertyIRAndState(propIR, errors));
                    }
                }
                resultControlInfo.Rules = properties.ToArray();
                bool hasDynamicProperties = isInResponsiveLayout && dynamicProperties.Any();
                resultControlInfo.DynamicProperties = hasDynamicProperties ? dynamicProperties.ToArray() : null;
                resultControlInfo.HasDynamicProperties = hasDynamicProperties;
                resultControlInfo.AllowAccessToGlobals = templateState?.ComponentManifest?.AllowAccessToGlobals;
            }
            resultControlInfo.Template = template;
            resultControlInfo.Children = orderedChildren;

            return (resultControlInfo, state?.ParentIndex ?? -1);
        }

        private static void RepopulateTemplateCustomProperties(FunctionNode func, CombinedTemplateState templateState, ErrorContainer errors, Entropy entropy)
        {
            var funcName = func.Identifier;
            var customProp = templateState.CustomProperties.FirstOrDefault(prop => prop.Name == funcName);
            if (customProp == default)
            {
                errors.ParseError(func.SourceSpan.GetValueOrDefault(), "Functions are not yet supported without corresponding custom properties in ControlTemplates.json");
                throw new DocumentException();
            }

            var scopeArgs = customProp.PropertyScopeKey.PropertyScopeRulesKey.ToDictionary(scope => scope.Name);
            var argTypes = func.Args.ToDictionary(arg => arg.Identifier, arg => arg.Kind.TypeName);

            int i = 1;
            foreach (var arg in func.Metadata)
            {
                if (arg.Identifier == PAConstants.ThisPropertyIdentifier)
                    continue;

                var propertyName = funcName + "_" + arg.Identifier;
                var defaultRule = entropy.GetDefaultScript(propertyName, arg.Default.Expression);

                if (!scopeArgs.TryGetValue(propertyName, out var propScopeRule))
                {
                    errors.ParseError(func.SourceSpan.GetValueOrDefault(), "Functions are not yet supported without corresponding custom properties in ControlTemplates.json");
                    throw new DocumentException();
                }
                if (!argTypes.TryGetValue(arg.Identifier, out var propType) || !Enum.TryParse<PropertyDataType>(propType, out var propTypeEnum))
                {
                    errors.ParseError(func.SourceSpan.GetValueOrDefault(), "Function metadata blocks must correspond to a function parameter with a valid type");
                    throw new DocumentException();
                }

                propScopeRule.ScopeVariableInfo.DefaultRule = defaultRule;
                propScopeRule.ScopeVariableInfo.ParameterIndex = i;
                propScopeRule.ScopeVariableInfo.ParentPropertyName = funcName;
                propScopeRule.ScopeVariableInfo.ScopePropertyDataType = (int)propTypeEnum;

                ++i;
            }
        }

        private static ControlInfoJson.RuleEntry CombinePropertyIRAndState(PropertyNode node, ErrorContainer errors, ControlState state = null)
        {
            var propName = node.Identifier;
            var expression = node.Expression.Expression;

            if (state == null)
            {
                var property = new ControlInfoJson.RuleEntry();
                property.Property = propName;
                property.InvariantScript = expression;
                property.RuleProviderType = "Unknown";
                return property;
            }
            else
            {
                var property = GetPropertyEntry(state, errors, propName, expression);
                return property;
            }
        }

        private static ControlInfoJson.DynamicPropertyJson CombineDynamicPropertyIRAndState(PropertyNode node, ControlState state = null)
        {
            var propName = node.Identifier;
            var expression = node.Expression.Expression;

            if (state == null)
            {
                var property = new ControlInfoJson.RuleEntry();
                property.Property = propName;
                property.InvariantScript = expression;
                property.RuleProviderType = "Unknown";
                return new ControlInfoJson.DynamicPropertyJson() { PropertyName = propName, Rule = property };
            }
            else
            {
                var property = GetDynamicPropertyEntry(state, propName, expression);
                return property;
            }
        }

        private static ControlInfoJson.RuleEntry GetPropertyEntry(ControlState state, ErrorContainer errors, string propName, string expression)
        {
            var property = new ControlInfoJson.RuleEntry();
            property.Property = propName;
            property.InvariantScript = expression;

            PropertyState propState = null;
            if (state.Properties != null && state.Properties.ToDictionary(prop => prop.PropertyName).TryGetValue(propName, out propState))
            {
                property.ExtensionData = propState.ExtensionData;
                property.NameMap = propState.NameMap;
                property.RuleProviderType = propState.RuleProviderType;
            }
            else
            {
                if (state.IsComponentDefinition ?? false)
                {
                    errors.UnsupportedOperationError("This tool currently does not support adding new custom properties to components. Please use Power Apps Studio to edit component definitions");
                    throw new DocumentException();
                }

                property.RuleProviderType = "Unknown";
            }

            return property;
        }

        private static ControlInfoJson.DynamicPropertyJson GetDynamicPropertyEntry(ControlState state, string propName, string expression)
        {
            var property = new ControlInfoJson.DynamicPropertyJson();
            property.PropertyName = propName;

            DynamicPropertyState propState = null;
            if (state.DynamicProperties.ToDictionary(prop => prop.PropertyName).TryGetValue(propName, out propState))
            {
                property.Rule = new ControlInfoJson.RuleEntry()
                {
                    InvariantScript = expression,
                    Property = propName,
                    ExtensionData = propState.Property.ExtensionData,
                    NameMap = propState.Property.NameMap,
                    RuleProviderType = propState.Property.RuleProviderType
                };
                property.ExtensionData = propState.ExtensionData;
            }
            else
            {
                property.Rule = new ControlInfoJson.RuleEntry()
                {
                    InvariantScript = expression,
                    RuleProviderType = "Unknown"
                };
            }

            return property;
        }



        private static readonly string CustomControlTemplateId = "Microsoft.PowerApps.CustomControlTemplate";
        // These two functions (split and recombine CustomTemplates) are responsible for handling the legacy DataTable control's
        // CustomControlDefinitionJson. This JSON contains a stamp of which version it was created in, but that is the only difference
        // As such, they were safe to move to Entropy. If entropy is removed, the one in the TemplateStore works fine for all instances
        // of the control.
        private static void SplitCustomTemplates(Entropy entropy, ControlInfoJson.Template controlTemplate, string controlName)
        {
            if (controlTemplate.Id != CustomControlTemplateId)
                return;

            entropy.AddDataTableControlJson(controlName, controlTemplate.CustomControlDefinitionJson);
        }

        private static void RecombineCustomTemplates(Entropy entropy, ControlInfoJson.Template controlTemplate, string controlName)
        {
            if (controlTemplate.Id != CustomControlTemplateId)
                return;

            if (entropy.TryGetDataTableControlJson(controlName, out var customTemplateJson))
            {
                controlTemplate.CustomControlDefinitionJson = customTemplateJson;
            }
        }
    }
}
