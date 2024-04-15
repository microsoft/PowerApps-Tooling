// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public class BuiltInTemplates
{
    public static readonly (string Name, string Id) App = ("Appinfo", WellKnownTemplateIds.AppInfo);
    public static readonly (string Name, string Id) Host = ("HostControl", WellKnownTemplateIds.HostControl);
    public static readonly (string Name, string Id) Screen = ("Screen", WellKnownTemplateIds.Screen);

    public static readonly (string Name, string Id) Component = ("Component", WellKnownTemplateIds.Component);
    public static readonly (string Name, string Id) FunctionComponent = ("FunctionComponent", WellKnownTemplateIds.FunctionComponent);
    public static readonly (string Name, string Id) DataComponent = ("DataComponent", WellKnownTemplateIds.DataComponent);
    public static readonly (string Name, string Id) CommandComponent = ("CommandComponent", WellKnownTemplateIds.CommandComponent);

    // Group is the legacy group container template
    public static readonly (string Name, string Id) Group = ("Group", WellKnownTemplateIds.Group);

    // Group Container is the newer layout container template
    public static readonly (string Name, string Id) GroupContainer = ("GroupContainer", WellKnownTemplateIds.GroupContainer);
}
