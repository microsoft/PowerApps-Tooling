// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
