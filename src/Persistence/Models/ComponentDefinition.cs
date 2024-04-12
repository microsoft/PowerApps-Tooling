// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// This class is meant to describe a Component definition.
/// </summary>
[YamlSerializable]
public record ComponentDefinition : Control
{
    protected ComponentDefinition() { }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [SetsRequiredMembers]
    public ComponentDefinition(string name, string variant, ControlTemplate template)
        : base(name, variant, template)
    {
    }

    [YamlMember(Order = 97)]
    public string? Description { get; set; }

    /// <summary>
    /// CanvasComponent, DataComponent, FunctionComponent, CommandComponent
    /// </summary>
    [YamlMember(Order = 98)]
    public ComponentType Type { get; set; } = ComponentType.Canvas;

    [YamlMember(Order = 99)]
    public bool AccessAppScope { get; set; }

    [YamlMember(Order = 100)]
    public IList<CustomProperty> CustomProperties { get; set; } = new List<CustomProperty>();

    internal override void AfterCreate(Dictionary<string, object?> controlDefinition)
    {
        if (controlDefinition.TryGetValue<IList<CustomProperty>>(nameof(CustomProperties), out var customProperties))
        {
            if (customProperties != null)
                CustomProperties = customProperties;
        }

        if (controlDefinition.TryGetValue<string>(nameof(Description), out var description))
        {
            Description = description;
        }

        if (controlDefinition.TryGetValue<string>(nameof(AccessAppScope), out var tmpString) &&
            bool.TryParse(tmpString, out var accessAppScope))
        {
            AccessAppScope = accessAppScope;
        }
    }
}
