// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Callbacks;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[DebuggerDisplay("{Template?.DisplayName}: {Name}")]
public abstract record Control
{
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

    private string _name = string.Empty;
    /// <summary>
    /// the control's name.
    /// </summary>
    [YamlMember(Order = 1)]
    public required string Name
    {
        get => _name;
        set
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
    public ControlPropertiesCollection Properties { get; set; } = new();

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


    internal virtual void AfterCreate(Dictionary<string, object?> controlDefinition)
    {
    }

    /// <summary>
    /// Called before serialization to hide nested templates which add properties to parent from YAML output.
    /// </summary>
    internal void HideNestedTemplates()
    {
        if (_children == null)
            return;

        for (var i = 0; i < _children.Count; i++)
        {
            if (_children[i].Template.AddPropertiesToParent)
            {
                foreach (var childTemplateProperty in _children[i].Properties)
                {
                    Properties.Add(childTemplateProperty.Key, childTemplateProperty.Value);
                }
                _children.RemoveAt(i);
                i--;
            }
        }
    }
}
