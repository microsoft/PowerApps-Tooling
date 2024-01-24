// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

//TODO: abstract?
internal record Control
{
    // TODO: rename to "Control" in yaml
    // TODO: make this a string and handle parsing/matchin later
    /// <summary>
    /// template uri of the control.
    /// </summary>
    public Uri? ControlUri { get; init; }

    /// <summary>
    /// the control's name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// key/value pairs of Control properties. Mapped to/from Control rules.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }

    /// <summary>
    /// list of child controls nested under this control.
    /// </summary>
    public Control[]? Controls { get; init; }
}
