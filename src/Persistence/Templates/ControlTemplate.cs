// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

[DebuggerDisplay("{Name}")]
public record ControlTemplate
{
    public ControlTemplate()
    {
    }

    /// <summary>
    /// Custom templates might have only id.
    /// </summary>
    /// <param name="id"></param>
    [SetsRequiredMembers]
    public ControlTemplate(string id)
    {
        Id = id;
        Name = id;
    }
    public required string Id { get; init; }

    private string _name = string.Empty;

    public required string Name
    {
        get => _name;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(Name));

            _name = value.Trim().FirstCharToUpper();
        }
    }

    private string? _displayName;

    public string DisplayName
    {
        get => string.IsNullOrWhiteSpace(_displayName) ? _name : _displayName;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                _displayName = null;
            else
                _displayName = value.Trim().FirstCharToUpper();
        }
    }

    public bool HasDisplayName => !string.IsNullOrWhiteSpace(_displayName);
}
