// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// Per control, this is the Power Apps Studio state content that doesn't impact app functionality like IsLocked
/// </summary>
public record ControlEditorState
{
    /// <summary>
    /// Constructor for serialization.
    /// </summary>
    public ControlEditorState()
    {
    }

    [SetsRequiredMembers]
    public ControlEditorState(Control control)
    {
        Name = control.Name;
        Template = control.Template;
    }

    /// <summary>
    /// Name.
    /// </summary>
    public required string Name { get; init; }

    public bool IsLocked { get; init; }

    /// <summary>
    /// List of child control editor state nested under this control.
    /// </summary>
    [JsonPropertyName("Children")]
    public IList<ControlEditorState>? Controls { get; set; }

    /// <summary>
    /// Temporary duplicated in the control editor state.
    /// </summary>
    public ControlTemplate? Template { get; init; }
}
