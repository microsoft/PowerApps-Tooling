// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool
{
    internal static class Diff
    {
        public static IEnumerable<IDelta> ComputeDelta(CanvasDocument parent, CanvasDocument child)
        {
            var delta = new List<IDelta>();
            AddControlDeltas(parent, child, parent._screens, child._screens, isComponents: false, delta);
            AddControlDeltas(parent, child, parent._components, child._components, isComponents: true, delta);
            AddTemplateDeltas(parent, child, delta);
            AddResourceDeltas(parent, child, delta);
            AddDataSourceDeltas(parent, child, delta);
            AddSettingsDelta(parent, child, delta);
            AddThemeDelta(parent, child, delta);

            return delta;
        }

        private static void AddControlDeltas(CanvasDocument parent, CanvasDocument child, Dictionary<string, IR.BlockNode> parentControlSet, Dictionary<string, IR.BlockNode> childControlSet, bool isComponents, List<IDelta> deltas)
        {

            foreach (var originalTopParentControl in parentControlSet)
            {
                if (childControlSet.TryGetValue(originalTopParentControl.Key, out var childScreen))
                {
                    deltas.AddRange(ControlDiffVisitor.GetControlDelta(childScreen, originalTopParentControl.Value, parent._editorStateStore, isComponents));
                }
                else
                {
                    deltas.Add(new RemoveControl(ControlPath.Empty, originalTopParentControl.Key, isComponents));
                }
            }
            foreach (var newScreen in childControlSet.Where(kvp => !parentControlSet.ContainsKey(kvp.Key)))
            {
                deltas.Add(new AddControl(ControlPath.Empty, newScreen.Value, child._editorStateStore.GetControlsWithTopParent(newScreen.Key).ToDictionary(state => state.Name), isComponents));
            }
        }

        private static void AddTemplateDeltas(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
        {
            var childClassicTemplatesDict = child._templates.UsedTemplates.ToDictionary(temp => temp.Name.ToLower());
            foreach (var template in child._templateStore.Contents)
            {
                if (parent._templateStore.TryGetTemplate(template.Key, out _))
                    continue;

                if (!template.Value.IsPcfControl)
                {
                    childClassicTemplatesDict.TryGetValue(template.Key.ToLower(), out var jsonTemplate);
                    deltas.Add(new AddTemplate(template.Key, template.Value, jsonTemplate));
                }
                else
                {
                    child._pcfControls.TryGetValue(template.Key, out var pcfTemplate);
                    deltas.Add(new AddTemplate(template.Key, template.Value, pcfTemplate));
                }
            }
        }

        private static void AddResourceDeltas(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
        {
            var childResourcesDict = child._resourcesJson.Resources.ToDictionary(resource => resource.Name);
            foreach (var resource in parent._resourcesJson.Resources)
            {
                if (childResourcesDict.ContainsKey(resource.Name))
                {
                    // $$$ Need resource contents diff here to detect resource replacements
                    // eg, delete old resource, add new resource with same name but different contents
                    // Use UpdateResource once implemented.
                    childResourcesDict.Remove(resource.Name);
                }
                else
                {
                    if (resource.ResourceKind == Schemas.ResourceKind.LocalFile)
                    {
                        var resourceName = resource.Path.Substring("Assets\\".Length);
                        deltas.Add(new RemoveResource(resource.Name, FilePath.FromMsAppPath(resourceName)));
                    }
                    else
                    {
                        deltas.Add(new RemoveResource(resource.Name));
                    }
                }
            }

            foreach (var remaining in childResourcesDict)
            {
                FileEntry file = null;
                if (remaining.Value.ResourceKind == Schemas.ResourceKind.LocalFile)
                {
                    var resourceName = remaining.Value.Path.Substring("Assets\\".Length);
                    child._assetFiles.TryGetValue(FilePath.FromMsAppPath(resourceName), out file);
                    deltas.Add(new AddResource(remaining.Key, remaining.Value, file));
                }
                else
                {
                    deltas.Add(new AddResource(remaining.Key, remaining.Value));
                }
            }
        }

        private static void AddDataSourceDeltas(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
        {
            foreach (var ds in child._dataSources)
            {
                if (!parent._dataSources.ContainsKey(ds.Key))
                    deltas.Add(new AddDataSource() { Name = ds.Key, Contents = ds.Value });
            }


            foreach (var ds in parent._dataSources)
            {
                if (!child._dataSources.ContainsKey(ds.Key))
                    deltas.Add(new RemoveDataSource() { Name = ds.Key });
            }
        }

        private static void AddSettingsDelta(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
        {
            // Use reflection to iterate over the keys in each document properties object and compare them
            var parentProps = parent._properties;
            var childProps = child._properties;

            var properties = typeof(DocumentPropertiesJson).GetProperties();
            foreach (PropertyInfo pi in properties)
            {
                var propName = pi.Name;
                // Diff ExtensionData separately
                if (propName == "ExtensionData")
                    continue;

                var parentValue = pi.GetValue(parentProps);
                var childValue = pi.GetValue(childProps);


                bool areEqual;
                if (parentValue == null && childValue == null)
                {
                    areEqual = true;

                }
                else if (pi.PropertyType.IsArray)
                {
                    areEqual = parentValue != null &&
                                childValue != null &&
                                Enumerable.SequenceEqual<object>(parentValue as object[], childValue as object[]);
                }
                else
                {
                    areEqual = Equals(parentValue, childValue);
                }

                if (!areEqual)
                {
                    deltas.Add(new DocumentPropertiesChange(propName, childValue));
                }
            }

            foreach (var field in parent._properties.ExtensionData)
            {
                if (!child._properties.ExtensionData.TryGetValue(field.Key, out var value))
                {
                    deltas.Add(new DocumentPropertiesChange(field.Key, wasRemoved: true));
                    continue;
                }

                if (value.ToString() == field.Value.ToString())
                    continue;

                deltas.Add(new DocumentPropertiesChange(field.Key, value));
            }

            foreach (var field in child._properties.ExtensionData.Where(kvp => !parent._properties.ExtensionData.ContainsKey(kvp.Key)))
            {
                // Added properties
                deltas.Add(new DocumentPropertiesChange(field.Key, field.Value));
            }
        }

        private static void AddThemeDelta(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
        {
            // Just replace the themes at this point.
            deltas.Add(new ThemeChange(child._themes));
        }
    }
}
