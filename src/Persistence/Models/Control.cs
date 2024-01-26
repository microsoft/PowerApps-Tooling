// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

public abstract record Control
{
    /// <summary>
    /// template uri of the control.
    /// </summary>
    public string? ControlUri { get; init; }

    /// <summary>
    /// the control's name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// key/value pairs of Control properties. Mapped to/from Control rules.
    /// </summary>
    public ControlPropertiesCollection Properties { get; init; } = new();

    /// <summary>
    /// list of child controls nested under this control.
    /// </summary>
    public Control[]? Controls { get; init; } = Array.Empty<Control>();
}
