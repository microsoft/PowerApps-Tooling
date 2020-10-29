// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // To use a DataComponents within the msapp, there must be an instance of it. 
    // The instance is highly redundant with the template definition, and 
    // includes 2 extra copies of the formulas.
    // This transform removes the redundant scripts. 
    //internal static class TranformDCInstances
    //{
    //    // DC template formulas are repeated twice in the ControlInfo Json 
    //    // At this point, all Templates are read in 
    //    public static void TransformTemplatesOnLoad(this CanvasDocument app)
    //    {
    //        foreach (var control in app._sources.Values)
    //        {
    //            foreach (var child in control.Value.TopParent.Children)
    //            {
    //                if (child.Template.Id == ControlInfoJson.Template.DataComponentId)
    //                {
    //                    string templateGuid = child.Template.Name;

    //                    MinDataComponentManifest dc;
    //                    if (app._dataComponents.TryGetValue(templateGuid, out dc))
    //                    {
    //                        var original = child.Template;
                                                        
    //                        // Clear out redundant fields. 
    //                        // Save "Essential" fields needed to reconstruct. 
    //                        // The source doesn't need a full copy of the entire template. 
    //                        child.Template = new ControlInfoJson.Template
    //                        {
    //                            Id = original.Id,
    //                            Name = original.Name,
    //                            Version = original.Version,
    //                            LastModifiedTimestamp = original.LastModifiedTimestamp,
    //                            IsComponentDefinition = false,
    //                            ComponentDefinitionInfo = new ComponentDefinitionInfoJson
    //                            {
    //                                ControlPropertyState = original.ComponentDefinitionInfo.ControlPropertyState,
    //                                Children = original.ComponentDefinitionInfo.Children
    //                            }                               
    //                        }; 


    //                        var templateRules = dc._sources.TopParent.GetRules();

    //                        // Remove rules if they're a dup 
    //                        var rules = new Dictionary<string, ControlInfoJson.RuleEntry>();
    //                        foreach (var rule in child.Rules)
    //                        {
    //                            ControlInfoJson.RuleEntry templateRule;
    //                            if (templateRules.TryGetValue(rule.Property, out templateRule))
    //                            {
    //                                if (templateRule.InvariantScript == rule.InvariantScript)
    //                                {
    //                                    // Rule is a dup of what's in template. Don't repeat it. 
    //                                    continue;
    //                                }
    //                            }
    //                            rules[rule.Property] = rule;
    //                        }

    //                        child.Rules = rules.Values.ToArray();
    //                    }

    //                }
    //            }
    //        }
    //    }

    //    // If this sourceFile contains a data-component instance, 
    //    // and it was minifed, rehydrate it.
    //    public static SourceFile RehydrateOnSave(this CanvasDocument app, SourceFile sf)
    //    {
    //        // foreach (var control in app._sources.Values)
    //        var control = sf.JsonClone();
    //        {
    //            foreach (var child in control.Value.TopParent.Children)
    //            {
    //                if (child.Template.Id == ControlInfoJson.Template.DataComponentId)
    //                {
    //                    string templateGuid = child.Template.Name;

    //                    MinDataComponentManifest dc;
    //                    if (app._dataComponents.TryGetValue(templateGuid, out dc))
    //                    {
    //                        var backup = child.Template;

    //                        // Copy the template from the DataComponent's definition
    //                        child.Template = dc._sources.TopParent.Template.JsonClone();

    //                        child.Template.Version = backup.Version;
    //                        child.Template.LastModifiedTimestamp = backup.LastModifiedTimestamp;
    //                        child.Template.IsComponentDefinition = false;
    //                        child.Template.ComponentDefinitionInfo = new ComponentDefinitionInfoJson
    //                        {
    //                            Name = dc.Name,
    //                            LastModifiedTimestamp = backup.LastModifiedTimestamp,
    //                            Rules = dc._sources.TopParent.Rules,
    //                            ControlPropertyState = backup.ComponentDefinitionInfo.ControlPropertyState,
    //                            Children = backup.ComponentDefinitionInfo.Children
    //                        };

    //                        var rules = dc._sources.TopParent.GetRules();
    //                        foreach (var rule in child.Rules)
    //                        {
    //                            rules[rule.Property] = rule;
    //                        }
    //                        child.Rules = rules.Values.ToArray();

    //                    }
    //                }
    //            }
    //        }

    //        return control;
    //    }
    //}
}
