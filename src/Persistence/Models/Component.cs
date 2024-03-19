// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(templateName: BuiltInTemplates.Component)]
[YamlSerializable]
public record Component : Control
{
    protected Component() { }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [SetsRequiredMembers]
    public Component(string name, string variant, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Variant = variant;
        Template = controlTemplateStore.GetByName(BuiltInTemplates.Component);
    }

    [YamlMember(Order = 4)]
    public IList<CustomProperty> CustomProperties { get; init; } = new List<CustomProperty>();
}
