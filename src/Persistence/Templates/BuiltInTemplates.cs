// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public static class BuiltInTemplates
{
    public static readonly (string Name, string Id) App = (BuiltInTemplateNames.App, BuiltInTemplateIds.App);
    public static readonly (string Name, string Id) Host = (BuiltInTemplateNames.Host, BuiltInTemplateIds.Host);
    public static readonly (string Name, string Id) Screen = (BuiltInTemplateNames.Screen, BuiltInTemplateIds.Screen);

    public static readonly (string Name, string Id) Component = (BuiltInTemplateNames.Component, BuiltInTemplateIds.Component);
    public static readonly (string Name, string Id) FunctionComponent = (BuiltInTemplateNames.FunctionComponent, BuiltInTemplateIds.FunctionComponent);
    public static readonly (string Name, string Id) DataComponent = (BuiltInTemplateNames.DataComponent, BuiltInTemplateIds.DataComponent);
    public static readonly (string Name, string Id) CommandComponent = (BuiltInTemplateNames.CommandComponent, BuiltInTemplateIds.CommandComponent);

    // Group is the legacy group container template
    public static readonly (string Name, string Id) Group = (BuiltInTemplateNames.Group, BuiltInTemplateIds.Group);

    // Group Container is the newer layout container template
    public static readonly (string Name, string Id) GroupContainer = (BuiltInTemplateNames.GroupContainer, BuiltInTemplateIds.Group);
}
