// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.AppMagic.Authoring.Persistence;
using Microsoft.PowerPlatform.Formulas.Tools.IO;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;

namespace Microsoft.PowerPlatform.Formulas.Tools.MergeTool;

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
        AddConnectionDeltas(parent, child, delta);
        AddSettingsDelta(parent, child, delta);
        AddThemeDelta(parent, child, delta);
        AddScreenOrderDelta(parent, child, delta);

        return delta;
    }

    private static void AddControlDeltas(CanvasDocument parent, CanvasDocument child, Dictionary<string, IR.BlockNode> parentControlSet, Dictionary<string, IR.BlockNode> childControlSet, bool isComponents, List<IDelta> deltas)
    {
        foreach (var originalTopParentControl in parentControlSet)
        {
            if (childControlSet.TryGetValue(originalTopParentControl.Key, out var childScreen))
            {
                deltas.AddRange(ControlDiffVisitor.GetControlDelta(childScreen, originalTopParentControl.Value, child._editorStateStore, parent._templateStore, child._templateStore, isComponents));
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

    // Look for change from res1 (before) to res2 (after).
    // Return null if same. Else return an update object if different. 
    private static UpdateResource TryGetResourceUpdateDelta(Schemas.ResourceJson res1, Schemas.ResourceJson res2, CanvasDocument doc1, CanvasDocument doc2)
    {
        // check the many individual flags. 
        var diffFlags = Schemas.ResourceJson.ResourcesMayBeDifferent(res1, res2);

        // Check actual contents. Ie, in case an image has been replaced.
        var path = res1.GetPath();

        Contract.Assert(path.Equals(res2.GetPath()));

        doc1._assetFiles.TryGetValue(path, out var file1);
        doc2._assetFiles.TryGetValue(path, out var file2);

        bool diffContent;
        if (file1 == null)
        {
            diffContent = file2 != null;
        }
        else if (file2 == null)
        {
            diffContent = true;
        }
        else
        {
            diffContent = !Utilities.ByteArrayCompare(file1.RawBytes, file2.RawBytes);
        }

        if (diffFlags || diffContent)
        {
            return new UpdateResource(path, res2, file2);
        }

        return null; // no diff
    }

    private static void AddResourceDeltas(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
    {
        var childResourcesDict = child._resourcesJson.Resources.ToDictionary(resource => resource.Name);
        foreach (var resource in parent._resourcesJson.Resources)
        {
            if (childResourcesDict.TryGetValue(resource.Name, out var resource2))
            {
                var changed = TryGetResourceUpdateDelta(resource, resource2, parent, child);

                if (changed != null)
                {
                    deltas.Add(changed);
                }

                // Same resource in parent and child - no change. 
                childResourcesDict.Remove(resource.Name);
            }
            else
            {
                if (resource.ResourceKind == Schemas.ResourceKind.LocalFile)
                {
                    var path = resource.GetPath();
                    deltas.Add(new RemoveResource(resource.Name, path));
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
                var path = remaining.Value.GetPath();
                child._assetFiles.TryGetValue(path, out file);
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

    private static void AddConnectionDeltas(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
    {
        foreach (var connection in child._connections ?? Enumerable.Empty<KeyValuePair<string, ConnectionJson>>())
        {
            if (!parent._connections.ContainsKey(connection.Key))
                deltas.Add(new AddConnection() { Name = connection.Key, Contents = connection.Value });
        }


        foreach (var connection in parent._connections ?? Enumerable.Empty<KeyValuePair<string, ConnectionJson>>())
        {
            if (!child._connections.ContainsKey(connection.Key))
                deltas.Add(new RemoveConnection() { Name = connection.Key });
        }
    }

    private static void AddSettingsDelta(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
    {
        // Use reflection to iterate over the keys in each document properties object and compare them
        var parentProps = parent._properties;
        var childProps = child._properties;

        var properties = typeof(DocumentPropertiesJson).GetProperties();
        foreach (var pi in properties)
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
                            Enumerable.SequenceEqual(parentValue as object[], childValue as object[]);
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

    private static void AddScreenOrderDelta(CanvasDocument parent, CanvasDocument child, List<IDelta> deltas)
    {
        if (parent._screenOrder.SequenceEqual(child._screenOrder))
            return;
        deltas.Add(new ScreenOrderChange(child._screenOrder));
    }
}
