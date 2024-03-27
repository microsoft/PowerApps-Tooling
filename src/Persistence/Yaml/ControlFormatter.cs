// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

/// <summary>
/// Utility to handle the changes to the Control pre-serialize and post-deserialize
/// for handling Z-Index, ordering, and nested template.
/// This is intended to be temporary, replaced by the conversion to another lower model.
/// </summary>
public static class ControlFormatter
{
    public static T BeforeSerialize<T>(this T control) where T : Control
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        var childrenToRemove = (control.Children ?? Enumerable.Empty<Control>()).Where(c => c.Template.AddPropertiesToParent).ToList();
        var propertiesToMerge = childrenToRemove.SelectMany(c => c.Properties).ToList();

        // Remove children to be merged, and sort by Z-Index
        var children = (control.Children ?? Enumerable.Empty<Control>())
            .Except(childrenToRemove)
            .OrderByDescending(c => c.ZIndex)
            .Select(BeforeSerialize) // Recurse into child controls
            .ToList();

        // Remove the Z-index property and merge in any needed properties from nested data controls
        var properties = new ControlPropertiesCollection(
            control.Properties
                .Where(kvp => kvp.Key != PropertyNames.ZIndex)
                .Concat(propertiesToMerge));

        return control with { Children = children, Properties = properties };
    }

    public static T AfterDeserialize<T>(this T control, IControlFactory controlFactory) where T : Control
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        // No processing needed if there are no children, or if the control is an App
        if (control is App || control.Children == null || !control.Children.Any())
        {
            return control;
        }

        // Create any children from nested templates
        var (childrenToAdd, propertiesToRemove) = RestoreNestedTemplates(control, controlFactory);

        var originalChildCount = control.Children.Count;

        var children = control.Children
            .Select(c => AfterDeserialize(c, controlFactory)) // Recurse into child controls
            .Select(addZIndex)
            .Concat(childrenToAdd)
            .ToList();

        var properties = propertiesToRemove.Count > 0
            ? new ControlPropertiesCollection(control.Properties.ExceptBy(propertiesToRemove, kvp => kvp.Key))
            : control.Properties;

        return control with { Children = children, Properties = properties };

        Control addZIndex(Control control, int orderedIndex)
        {
            // Controls are sorted in descending Z-Index order
            var zIndex = originalChildCount - orderedIndex;
            var zIndexProp = new ControlProperty(PropertyNames.ZIndex, zIndex.ToString(CultureInfo.InvariantCulture));

            var newProperties = new ControlPropertiesCollection(
                control.Properties.Append(KeyValuePair.Create(PropertyNames.ZIndex, zIndexProp)));

            return control with { Properties = newProperties };
        }
    }

    private static (List<Control> childrenToAdd, List<string> propertiesToRemove) RestoreNestedTemplates(Control control, IControlFactory controlFactory)
    {
        var childrenToAdd = (control.Template.NestedTemplates ?? Enumerable.Empty<ControlTemplate>())
            .Where(t => t.AddPropertiesToParent)
            .Select(nestedTemplate =>
                controlFactory.Create(Guid.NewGuid().ToString(), nestedTemplate,
                    properties: new ControlPropertiesCollection(
                        nestedTemplate.InputProperties
                            .Where(prop => control.Properties.ContainsKey(prop.Key))
                            .Select(prop => KeyValuePair.Create(prop.Key, control.Properties[prop.Key]))))
            )
            .ToList();

        var propertiesToRemove = childrenToAdd.SelectMany(c => c.Properties.Keys).ToList();
        return (childrenToAdd, propertiesToRemove);
    }
}
