// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// Per control, this is the Power Apps Studio state content that doesn't impact app functionality like IsLocked
/// </summary>
public record ControlEditorState
{
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
}
