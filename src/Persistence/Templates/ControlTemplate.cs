// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

[DebuggerDisplay("{FullName} c:{IsClassic} id:{Id}")]
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
        _invariantName = id;
    }

    /// <summary>
    /// Id of the template. For example, 'http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas', etc.
    /// </summary>
    public required string Id { get; init; }

    private string _invariantName = string.Empty;

    /// <summary>
    /// Name used to persist in YAML. For example, 'Button', 'ButtonCanvas', etc.
    /// </summary>
    public string InvariantName
    {
        get => _invariantName;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(InvariantName));

            _invariantName = value.Trim().FirstCharToUpper();
        }
    }

    private string _fullName = string.Empty;

    /// <summary>
    /// Name in the template store. For example, 'button', 'PowerApps_CoreControls_ButtonCanvas', etc.
    /// </summary>
    public string FullName
    {
        get => !string.IsNullOrWhiteSpace(_fullName) ? _fullName : _invariantName;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(FullName));

            _fullName = value.Trim().FirstCharToUpper();
        }
    }

    private string? _displayName;

    public string DisplayName
    {
        get => string.IsNullOrWhiteSpace(_displayName) ? _invariantName : _displayName;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                _displayName = null;
            else
                _displayName = value.Trim().FirstCharToUpper();
        }
    }

    public bool HasDisplayName => !string.IsNullOrWhiteSpace(_displayName);

    /// <summary>
    /// True for classic controls, false for modern controls and all other control types.
    /// </summary>
    public bool IsClassic { get; init; }

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
