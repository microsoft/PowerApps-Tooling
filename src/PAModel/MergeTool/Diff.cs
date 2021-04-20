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
            foreach (var originalScreen in parent._screens)
            {
                if (child._screens.TryGetValue(originalScreen.Key, out var childScreen))
                {
                    delta.AddRange(ControlDiffVisitor.GetControlDelta(childScreen, originalScreen.Value, parent._editorStateStore));
                }
                else
                {
                    delta.Add(new RemoveControl() { ControlName = originalScreen.Key, ParentControlPath = ControlPath.Empty });
                }
            }
            foreach (var newScreen in child._screens.Where(kvp => !parent._screens.ContainsKey(kvp.Key)))
            {
                delta.Add(new AddControl() { Control = newScreen.Value, ControlStates = child._editorStateStore.GetControlsWithTopParent(newScreen.Key).ToDictionary(state => state.Name), ParentControlPath = ControlPath.Empty });
            }

            var childTemplatesDict = child._templates.UsedTemplates.ToDictionary(temp => temp.Name.ToLower());
            foreach (var template in child._templateStore.Contents)
            {
                if (parent._templateStore.TryGetTemplate(template.Key, out _))
                    continue;

                childTemplatesDict.TryGetValue(template.Key.ToLower(), out var jsonTemplate); 

                delta.Add(new AddTemplate() { Name = template.Key, Template = template.Value, JsonTemplate = jsonTemplate });
            }

            AddResourceDeltas(parent, child, delta);

            AddDataSourceDeltas(parent, child, delta);

            return delta;
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

                    childResourcesDict.Remove(resource.Name);
                }

                // We don't care about the asset files for remove for now,
                // studio just won't load ones that don't have a ref in the json
                // And they'll get cleaned up next save
                // $$$ If we want a clean git history, we should apply that here
                deltas.Add(new RemoveResource() { Name = resource.Name });
            }

            foreach (var remaining in childResourcesDict)
            {
                FileEntry file = null;
                if (remaining.Value.ResourceKind == "LocalFile")
                {
                    var resourceName = remaining.Value.Path.Substring("Assets\\".Length);
                    child._assetFiles.TryGetValue(FilePath.FromMsAppPath(resourceName), out file);
                }
                deltas.Add(new AddResource() { Name = remaining.Key, Resource = remaining.Value, File = file});
            }
        }

        private static void AddDataSourceDeltas(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
        {
            foreach (var ds in child._dataSources)
            {
                if (!parent._dataSources.ContainsKey(ds.Key))
                    deltas.Add(new AddDataSource() { Name = ds.Key, Contents = ds.Value });
            }
        }
    }
}
