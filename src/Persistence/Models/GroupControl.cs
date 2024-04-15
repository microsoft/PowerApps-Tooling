// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(templateName: "Group")]
[YamlSerializable]
public record GroupControl : Control
{
    public GroupControl()
    {
    }

    [SetsRequiredMembers]
    public GroupControl(string name, string variant, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Variant = variant;
        Template = controlTemplateStore.GetByName(BuiltInTemplates.Group.Name);
    }
}
