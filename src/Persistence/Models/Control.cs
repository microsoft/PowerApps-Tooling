// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[DebuggerDisplay("{Template?.DisplayName}: {Name}")]
public abstract record Control
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We need both 'public init' and 'private set', which cannot be accomplished by auto property")]
    private IList<Control>? _children;

    public Control()
    {
    }

    [SetsRequiredMembers]
    public Control(string name, string variant, ControlTemplate template)
    {
        Name = name;
        Variant = variant;
        Template = template;
    }

    /// <summary>
    /// template uri of the control.
    /// </summary>
    [YamlMember(Alias = YamlFields.Control, Order = 0)]
    public string TemplateId => Template.Id;

    private readonly string _name = string.Empty;
    /// <summary>
    /// the control's name.
    /// </summary>
    [YamlMember(Order = 1)]
    public required string Name
    {
        get => _name;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(Name));

            _name = value.Trim();
        }
    }

    [YamlMember(Order = 2)]
    public required string Variant { get; init; } = string.Empty;

    [YamlMember(Order = 3)]
    public string Layout { get; set; } = string.Empty;

    /// <summary>
    /// key/value pairs of Control properties. Mapped to/from Control rules.
    /// </summary>
    [YamlMember(Order = 4)]
    public ControlPropertiesCollection Properties { get; init; } = new();

    /// <summary>
    /// list of child controls nested under this control.
    /// This collection can be null in cases where the control does not support children.
    /// </summary>
    [YamlMember(Order = 5)]
    public IList<Control>? Children { get => _children; set => _children = value; }

    [YamlIgnore]
    public ControlEditorState? EditorState { get; set; }

    [YamlIgnore]
    public required ControlTemplate Template { get; init; }

    [YamlIgnore]
    public int ZIndex
    {
        get
        {
            if (Properties.TryGetValue(PropertyNames.ZIndex, out var prop) && int.TryParse(prop.Value, out var zIndex))
                return zIndex;

            return int.MaxValue;

        }
    }
}
