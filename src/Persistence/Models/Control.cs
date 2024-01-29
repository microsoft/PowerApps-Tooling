// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[DebuggerDisplay("{Name}")]
public abstract record Control
{
    public Control()
    {
    }

    [SetsRequiredMembers]
    public Control(string name)
    {
        Name = name;
        ControlUri = BuiltInTemplates.HostControl;
    }

    /// <summary>
    /// template uri of the control.
    /// </summary>
    public required string ControlUri { get; init; }

    private readonly string _name = string.Empty;
    /// <summary>
    /// the control's name.
    /// </summary>
    public required string Name
    {
        get => _name;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(Name));

            _name = value;
        }
    }

    /// <summary>
    /// key/value pairs of Control properties. Mapped to/from Control rules.
    /// </summary>
    public ControlPropertiesCollection Properties { get; init; } = new();

    /// <summary>
    /// list of child controls nested under this control.
    /// </summary>    
    public Control[] Controls { get; init; } = Array.Empty<Control>();

    public ControlEditorState? EditorState { get; set; }
}
