// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public static class ModelsExtensions
{
    public static int GetDescendantsCount(this ScreenInstance screen)
    {
        _ = screen ?? throw new ArgumentNullException(nameof(screen));

        var count = 0;
        if (screen.Children?.Count > 0)
        {
            count += screen.Children.Count;

            foreach (var namedDict in screen.Children)
            {
                Debug.Assert(namedDict.Count == 1, "Each child of a screen should represent a dictionary with a single item.");
                var namedControl = namedDict.Single();

                count += namedControl.Value.GetDescendantsCount();
            }
        }

        return count;
    }

    public static int GetDescendantsCount(this ControlInstance control)
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        var count = 0;
        if (control.Children?.Count > 0)
        {
            count += control.Children.Count;

            foreach (var namedDict in control.Children)
            {
                Debug.Assert(namedDict.Count == 1, "Each child of a screen should represent a dictionary with a single item.");
                var namedControl = namedDict.Single();

                count += namedControl.Value.GetDescendantsCount();
            }
        }

        return count;
    }
}
