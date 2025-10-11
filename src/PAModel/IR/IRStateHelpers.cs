// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using System.Linq;
using System.Text.Json;
using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using static Microsoft.PowerPlatform.Formulas.Tools.ControlInfoJson;
using Microsoft.PowerPlatform.Formulas.Tools.Extensions;

namespace Microsoft.PowerPlatform.Formulas.Tools;

internal static class IRStateHelpers
{
    public const string ControlTemplateOverridableProperties = "OverridableProperties";
    public const string ControlTemplatePCFDynamicSchemaForIRRetrieval = "PCFDynamicSchemaForIRRetrieval";
    public const string HostControlTemplateName = "hostControl";

    internal static void SplitIRAndState(SourceFile file, EditorStateStore stateStore, TemplateStore templateStore, Entropy entropy, out BlockNode topParentIR)
    {
        var topParentJson = file.Value.TopParent;
        SplitIRAndState(topParentJson, topParentJson.Name, 0, stateStore, templateStore, entropy, out topParentIR);
    }

    private static void SplitIRAndState(Item control, string topParentName, int index, EditorStateStore stateStore, TemplateStore templateStore, Entropy entropy, out BlockNode controlIR)
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

                var customPropScopeRules = control.Template.CustomProperties
                    .Where(customProp => customProp.IsFunctionProperty)
                    .SelectMany(customProp =>
                        customProp.PropertyScopeKey.PropertyScopeRulesKey);

                // Skip component property params on instances
                customPropsToHide = new HashSet<string>(customPropScopeRules
                           .Select(propertyScopeRule => propertyScopeRule.Name));

                foreach (var arg in customPropScopeRules)
                {
                    var invariantScript = control.Rules.First(rule => rule.Property == arg.Name)?.InvariantScript;

                    // Handle the case where invariantScript value of the property is not same as the default script.
                    if (invariantScript != null && invariantScript != arg.ScopeVariableInfo.DefaultRule)
                    {
                        var argKey = $"{control.Name}.{arg.Name}";
                        entropy.FunctionParamsInvariantScriptsOnInstances.Add(argKey, new string[] { arg.ScopeVariableInfo.DefaultRule, invariantScript });
                    }
                }
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
                            var argKey = $"{control.Name}.{arg.Name}";
                            entropy.FunctionParamsInvariantScripts.Add(argKey, new string[] { arg.ScopeVariableInfo.DefaultRule, invariantScript });
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

        foreach (var property in control.DynamicProperties ?? Enumerable.Empty<DynamicPropertyJson>())
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

        // Storing Template PCFDynamicSchemaForIRRetrieval/OverridableProperties field values for each control instance.
        // Since this value could be different for each control instance though it follows same control template.
        // Eg:Control Instance 1 -> template1 -> PCFDynamicSchemaForIRRetrieval1/OverridableProperties1
        // Control Instance 2 -> template1 -> PCFDynamicSchemaForIRRetrieval2/OverridableProperties2

        if (control.Template.ExtensionData != null)
        {
            if (control.Template.ExtensionData.TryGetValue(ControlTemplatePCFDynamicSchemaForIRRetrieval, out var PCFVal))
            {
                entropy.PCFDynamicSchemaForIRRetrievalEntry.Add(control.Name, PCFVal);
            }
            if (control.Template.ExtensionData.TryGetValue(ControlTemplateOverridableProperties, out var OverridablePropVal))
            {
                entropy.OverridablePropertiesEntry.Add(control.Name, OverridablePropVal);
            }
        }

        // Store PCF control template data in entropy, per control.
        // Since this could be different for different controls, even if that appear to follow the same template
        // Eg:Control Instance 1 -> pcftemplate1 -> DynamicControlDefinitionJson1
        // Control Instance 2 -> pcftemplate1 -> DynamicControlDefinitionJson2
        // A copy of the template will also be kept in the template store, in case entropy data is lost.
        if (IsPCFControl(control.Template))
        {
            entropy.PCFTemplateEntry.Add(control.Name, control.Template);
        }

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
            templateState = new CombinedTemplateState(control.Template)
            {
                ComponentDefinitionInfo = control.Template.ComponentDefinitionInfo
            };
            var templateName = templateState.TemplateDisplayName ?? templateState.Name;
            // Template values could be different for each host control instances.
            // Considering that, we need to store each of these template values separately in templatestore, rather than once for hostcontrol. 
            // This enables Storing Template HostType and HostService details for each host control instances.
            // Example Scenarios:
            // Host Control Instance 1 -> template1 -> HostType1
            // Host Control Instance 2 -> template1 -> HostType2
            // OR
            // Host Control Instance 1 -> template1 -> HostService1
            // Host Control Instance 2 -> template1 -> HostService2
            if (IsHostControl(control.Template))
            {
                templateStore.AddTemplate(control.Name, templateState);
            }
            else
            {
                templateStore.AddTemplate(templateName, templateState);
            }
        }

        SplitCustomTemplates(entropy, control.Template, control.Name);

        if (int.TryParse(control.ControlUniqueId, out var controlId))
        {
            entropy.ControlUniqueIds.Add(control.Name, controlId);
        }
        else
        {
            entropy.ControlUniqueGuids.Add(control.Name, Guid.Parse(control.ControlUniqueId));
        }
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

    private static bool IsPCFControl(Template template)
    {
        if (template.Id == null)
            return false;

        return template.Id.StartsWith(Template.PcfControl);
    }

    private static bool IsHostControl(Template template)
    {
        if (template.Id == null)
            return false;

        return template.Id.StartsWith(Template.HostControl);
    }

    private static (PropertyNode prop, PropertyState state) SplitProperty(RuleEntry rule)
    {
        var script = rule.InvariantScript;
        var prop = new PropertyNode() { Expression = new ExpressionNode() { Expression = script }, Identifier = rule.Property };
        var state = new PropertyState() { PropertyName = rule.Property, ExtensionData = rule.ExtensionData, NameMap = rule.NameMap, RuleProviderType = rule.RuleProviderType, Category = rule.Category };
        return (prop, state);
    }

    private static (PropertyNode prop, DynamicPropertyState state) SplitDynamicProperty(DynamicPropertyJson dynamicProperty)
    {
        var (prop, propertyState) = SplitProperty(dynamicProperty.Rule);
        var state = new DynamicPropertyState() { PropertyName = propertyState.PropertyName, Property = propertyState, ExtensionData = dynamicProperty.ExtensionData, ControlPropertyState = dynamicProperty.ControlPropertyState };
        return (prop, state);
    }

    internal static SourceFile CombineIRAndState(BlockNode blockNode, ErrorContainer errors, EditorStateStore stateStore, TemplateStore templateStore, UniqueIdRestorer uniqueIdRestorer, Entropy entropy)
    {
        var topParentJson = CombineIRAndState(blockNode, errors, string.Empty, false, stateStore, templateStore, uniqueIdRestorer, entropy);
        return SourceFile.New(new ControlInfoJson() { TopParent = topParentJson.item });
    }

    // Returns pair of item and index (with respect to parent order)
    private static (Item item, int index) CombineIRAndState(BlockNode blockNode, ErrorContainer errors, string parent, bool isInResponsiveLayout, EditorStateStore stateStore, TemplateStore templateStore, UniqueIdRestorer uniqueIdRestorer, Entropy entropy)
    {
        var controlName = blockNode.Name.Identifier;
        var templateIR = blockNode.Name.Kind;
        var templateName = templateIR.TypeName;
        var variantName = templateIR.OptionalVariant;

        // Bottom up, merge children first
        var children = new List<(Item item, int index)>();
        foreach (var childBlock in blockNode.Children)
        {
            children.Add(CombineIRAndState(childBlock, errors, controlName, DynamicProperties.AddsChildDynamicProperties(templateName, variantName), stateStore, templateStore, uniqueIdRestorer, entropy));
        }

        var orderedChildren = children.OrderBy(childPair => childPair.index).Select(pair => pair.item).ToArray();

        Template template;
        CombinedTemplateState templateState;

        // Prefer the specific PCF template for the control, if available. Otherwise, try to fall back to the template store.
        if (entropy.PCFTemplateEntry.TryGetValue(controlName, out var PCFTemplate))
        {
            template = PCFTemplate;
            templateState = new CombinedTemplateState(PCFTemplate);
        }
        else if (templateName == HostControlTemplateName && templateStore.TryGetTemplate(controlName, out templateState))
        {
            template = templateState.ToControlInfoTemplate();
        }
        else if (!templateStore.TryGetTemplate(templateName, out templateState))
        {
            template = Template.CreateDefaultTemplate(templateName, null);
        }
        else
        {
            template = templateState.ToControlInfoTemplate();
        }

        RecombineCustomTemplates(entropy, template, controlName);

        var uniqueId = uniqueIdRestorer.GetControlId(controlName);
        Item resultControlInfo;
        if (stateStore.TryGetControlState(controlName, out var state))
        {
            var properties = new List<RuleEntry>();
            var dynamicProperties = new List<DynamicPropertyJson>();
            foreach (var propIR in blockNode.Properties)
            {
                // Dynamic properties could be null for the galleryTemplateTemplate                
                var isDynamicProperty = state.DynamicProperties != null &&
                    ((isInResponsiveLayout && DynamicProperties.IsResponsiveLayoutProperty(propIR.Identifier)) ||
                    // Check if property is dynamic (responsive layout or has metadata like ControlPropertyState)
                    state.DynamicProperties.Any(dp => dp.PropertyName == propIR.Identifier));

                if (isDynamicProperty)
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
                    var dummyProp = new DynamicPropertyJson() { PropertyName = dynPropState.PropertyName };

                    // Preserve ControlPropertyState if it exists
                    if (dynPropState.ControlPropertyState != null)
                    {
                        dummyProp.ControlPropertyState = JsonSerializer.SerializeToElement(dynPropState.ControlPropertyState);
                    }

                    dynamicProperties.Add(dummyProp);
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
                        var argKey = $"{controlName}.{propName}";
                        properties.Add(GetPropertyEntry(state, errors, propName, entropy.GetInvariantScript(argKey, arg.Default.Expression)));
                    }

                    RepopulateTemplateCustomProperties(func, templateState, errors, entropy, controlName);
                }
            }
            else if (template.CustomProperties?.Any(prop => prop.IsFunctionProperty) ?? false)
            {
                // For component uses, recreate the dummy props for function parameters
                foreach (var hiddenScopeRule in template.CustomProperties.Where(prop => prop.IsFunctionProperty).SelectMany(prop => prop.PropertyScopeKey.PropertyScopeRulesKey))
                {
                    if (!properties.Any(x => x.Property == hiddenScopeRule.Name))
                    {
                        var argKey = $"{controlName}.{hiddenScopeRule.Name}";
                        var script = entropy.GetInvariantScriptOnInstances(argKey, hiddenScopeRule.ScopeVariableInfo.DefaultRule);
                        properties.Add(GetPropertyEntry(state, errors, hiddenScopeRule.Name, script));
                    }
                }
            }

            // Preserve ordering from serialized IR
            // Required for roundtrip checks
            properties = properties.OrderBy(prop => state.Properties?.Select(propState => propState.PropertyName).ToList().IndexOf(prop.Property) ?? -1).ToList();
            resultControlInfo = new Item()
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
                AllowAccessToGlobals = (state.IsComponentDefinition ?? false) ? (templateState?.ComponentDefinitionInfo?.AllowAccessToGlobals) ?? state.AllowAccessToGlobals : state.AllowAccessToGlobals,
            };

            if (state.IsComponentDefinition ?? false)
            {
                // There seems to situation where ComponentManifest AllowAccessToGlobals is out of sync with instance AllowAccessToGlobals value
                // But ComponentDefinitionInfo AllowAccessToGlobals property value is the source of truth, hence using it
                // When reconstructing componentdefinition, we need to track whether msapp has AllowGlobalScope property in component instance
                // For this, we use state IsLegacyComponentAllowGlobalScopeCase.
                templateState.ComponentDefinitionInfo = new ComponentDefinitionInfoJson(resultControlInfo, template.LastModifiedTimestamp, orderedChildren, entropy.IsLegacyComponentAllowGlobalScopeCase ? null : (templateState.ComponentDefinitionInfo?.AllowAccessToGlobals ?? state.AllowAccessToGlobals));
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
            resultControlInfo = Item.CreateDefaultControl();
            resultControlInfo.Name = controlName;
            resultControlInfo.ControlUniqueId = uniqueId.ToString();
            resultControlInfo.Parent = parent;
            resultControlInfo.VariantName = variantName ?? string.Empty;
            resultControlInfo.StyleName = $"default{templateName.FirstCharToUpper()}Style";
            var properties = new List<RuleEntry>();
            var dynamicProperties = new List<DynamicPropertyJson>();
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
            var hasDynamicProperties = isInResponsiveLayout && dynamicProperties.Any();
            resultControlInfo.DynamicProperties = hasDynamicProperties ? dynamicProperties.ToArray() : null;
            resultControlInfo.HasDynamicProperties = hasDynamicProperties;
            resultControlInfo.AllowAccessToGlobals = templateState?.ComponentManifest?.AllowAccessToGlobals;
        }
        resultControlInfo.Template = template.JsonClone();
        resultControlInfo.Children = orderedChildren;

        // Using the stored PCFDynamicSchemaForIRRetrieval/OverridableProperties value for each control instance,
        // instead of the default value from the control template.
        if (entropy.OverridablePropertiesEntry.TryGetValue(controlName, out var OverridablePropVal))
        {
            resultControlInfo.Template.ExtensionData[ControlTemplateOverridableProperties] = OverridablePropVal;
        }
        if (entropy.PCFDynamicSchemaForIRRetrievalEntry.TryGetValue(controlName, out var PCFVal))
        {
            resultControlInfo.Template.ExtensionData[ControlTemplatePCFDynamicSchemaForIRRetrieval] = PCFVal;
        }

        return (resultControlInfo, state?.ParentIndex ?? -1);
    }

    private static void RepopulateTemplateCustomProperties(FunctionNode func, CombinedTemplateState templateState, ErrorContainer errors, Entropy entropy, string controlName)
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

        var i = 1;
        foreach (var arg in func.Metadata)
        {
            if (arg.Identifier == PAConstants.ThisPropertyIdentifier)
                continue;

            var propertyName = funcName + "_" + arg.Identifier;
            var argKey = $"{controlName}.{propertyName}";
            var defaultRule = entropy.GetDefaultScript(argKey, arg.Default.Expression);

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

    private static RuleEntry CombinePropertyIRAndState(PropertyNode node, ErrorContainer errors, ControlState state = null)
    {
        var propName = node.Identifier;
        var expression = node.Expression.Expression;

        if (state == null)
        {
            var property = new RuleEntry
            {
                Property = propName,
                InvariantScript = expression,
                RuleProviderType = "Unknown"
            };
            return property;
        }
        else
        {
            var property = GetPropertyEntry(state, errors, propName, expression);
            return property;
        }
    }

    private static DynamicPropertyJson CombineDynamicPropertyIRAndState(PropertyNode node, ControlState state = null)
    {
        var propName = node.Identifier;
        var expression = node.Expression.Expression;

        if (state == null)
        {
            var property = new RuleEntry
            {
                Property = propName,
                InvariantScript = expression,
                RuleProviderType = "Unknown"
            };
            return new DynamicPropertyJson() { PropertyName = propName, Rule = property };
        }
        else
        {
            var property = GetDynamicPropertyEntry(state, propName, expression);
            return property;
        }
    }

    private static RuleEntry GetPropertyEntry(ControlState state, ErrorContainer errors, string propName, string expression)
    {
        var property = new RuleEntry
        {
            Property = propName,
            InvariantScript = expression
        };

        if (state.Properties != null && state.Properties.ToDictionary(prop => prop.PropertyName).TryGetValue(propName, out var propState))
        {
            property.ExtensionData = propState.ExtensionData;
            property.NameMap = propState.NameMap;
            property.RuleProviderType = propState.RuleProviderType;
            property.Category = propState.Category;
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

    private static DynamicPropertyJson GetDynamicPropertyEntry(ControlState state, string propName, string expression)
    {
        var property = new DynamicPropertyJson
        {
            PropertyName = propName
        };

        if (state.DynamicProperties.ToDictionary(prop => prop.PropertyName).TryGetValue(propName, out var propState))
        {
            // The DynamicProperties may contain items without an existing corresponding property leading to empty property variables
            if (propState.Property != null)
            {
                property.Rule = new RuleEntry()
                {
                    InvariantScript = expression,
                    Property = propName,
                    Category = propState.Property.Category,
                    ExtensionData = propState.Property.ExtensionData,
                    NameMap = propState.Property.NameMap,
                    RuleProviderType = propState.Property.RuleProviderType
                };
                property.ExtensionData = propState.ExtensionData;
            }

            // Preserve ControlPropertyState
            if (propState.ControlPropertyState != null)
            {
                property.ControlPropertyState = JsonSerializer.SerializeToElement(propState.ControlPropertyState);
            }
        }
        else
        {
            property.Rule = new RuleEntry()
            {
                InvariantScript = expression,
                RuleProviderType = "Unknown"
            };
        }

        return property;
    }



    private const string CustomControlTemplateId = "Microsoft.PowerApps.CustomControlTemplate";
    // These two functions (split and recombine CustomTemplates) are responsible for handling the legacy DataTable control's
    // CustomControlDefinitionJson. This JSON contains a stamp of which version it was created in, but that is the only difference
    // As such, they were safe to move to Entropy. If entropy is removed, the one in the TemplateStore works fine for all instances
    // of the control.
    private static void SplitCustomTemplates(Entropy entropy, Template controlTemplate, string controlName)
    {
        if (controlTemplate.Id != CustomControlTemplateId)
            return;

        entropy.AddDataTableControlJson(controlName, controlTemplate.CustomControlDefinitionJson);
    }

    private static void RecombineCustomTemplates(Entropy entropy, Template controlTemplate, string controlName)
    {
        if (controlTemplate.Id != CustomControlTemplateId)
            return;

        if (entropy.TryGetDataTableControlJson(controlName, out var customTemplateJson))
        {
            controlTemplate.CustomControlDefinitionJson = customTemplateJson;
        }
    }
}
