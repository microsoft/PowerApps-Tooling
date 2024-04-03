// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Callbacks;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// This class is meant to describe a Component definition.
/// </summary>
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

        var baseTemplate = controlTemplateStore.GetByName(BuiltInTemplates.Component);
        Template = baseTemplate with { Name = name };
    }

    [YamlMember(Order = 100)]
    public CustomPropertiesCollection CustomProperties { get; init; } = new();

    [OnDeserialized]
    internal override void AfterDeserialize()
    {
        base.AfterDeserialize();

        if (CustomProperties != null)
        {
            foreach (var kv in CustomProperties)
            {
                kv.Value.Name = kv.Key;
            }
        }
    }
}
