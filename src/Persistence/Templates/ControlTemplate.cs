// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
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
    public required string Id { get; init; }

    public static implicit operator ControlTemplate(string id)
    {
        return new(id);
    }
}
