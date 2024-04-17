// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
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
        _name = id;
    }

    public required string Id { get; init; }

    private string _name = string.Empty;

    public string Name
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

    public bool AddPropertiesToParent { get; init; }

    public ControlPropertiesCollection InputProperties { get; init; } = new();

    public IList<ControlTemplate>? NestedTemplates { get; init; }

    public CustomComponentInfo? ComponentInfo { get; init; }

    [MemberNotNullWhen(true, nameof(ComponentInfo))]
    public bool IsCustomComponent => ComponentInfo != null;

    public bool IsPcfControlTemplate { get; init; }

    public class CustomComponentInfo
    {
        /// <summary>
        /// The unique id (a form of guid) of the component as serialized in the app.
        /// </summary>
        public required string UniqueId { get; init; }

        /// <summary>
        /// The friendly identifier of the component.
        /// </summary>
        public required string Name { get; init; }
    }
}
