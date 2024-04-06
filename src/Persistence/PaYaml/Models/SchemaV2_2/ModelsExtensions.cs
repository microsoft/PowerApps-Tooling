// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV2_2;

public static class ModelsExtensions
{
    public static int GetDescendantsCount(this ScreenInstance screen)
    {
        _ = screen ?? throw new ArgumentNullException(nameof(screen));

        return screen.Children?.Count > 0
            ? screen.Children.Count + screen.Children.Sum(namedControl => namedControl.Value.GetDescendantsCount())
            : 0;
    }

    public static int GetDescendantsCount(this ControlInstance control)
    {
        _ = control ?? throw new ArgumentNullException(nameof(control));

        return control.Children?.Count > 0
            ? control.Children.Count + control.Children.Sum(namedControl => namedControl.Value.GetDescendantsCount())
            : 0;
    }
}
