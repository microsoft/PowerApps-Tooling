// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

/// <summary>
/// Utility to handle the changes to the Control pre-serialize and post-deserialize
/// for handling Z-Index, ordering, and nested template.
/// This is intended to be temporary, replaced by the conversion to another lower model.
/// </summary>
public static class ControlFormatter
{
    public static Control BeforeSerialize(Control control)
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
}
