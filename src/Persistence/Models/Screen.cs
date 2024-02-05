// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(templateName: BuiltInTemplates.Screen)]
public record Screen : Control
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [SetsRequiredMembers]
    public Screen(string name, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Template = controlTemplateStore.GetByName(BuiltInTemplates.Screen);
    }
}
