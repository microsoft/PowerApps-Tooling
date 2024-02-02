// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

/// <summary>
/// Control template store.
/// </summary>
public class ControlTemplateStore : IControlTemplateStore
{
    private readonly SortedList<string, ControlTemplate> _controlTemplatesByName = new();
    private readonly SortedList<string, ControlTemplate> _controlTemplatesByUri = new();

    public void Add(ControlTemplate controlTemplate)
    {
        _ = controlTemplate ?? throw new ArgumentNullException(nameof(controlTemplate));

        _controlTemplatesByName.Add(controlTemplate.Name, controlTemplate);
        _controlTemplatesByUri.Add(controlTemplate.Uri, controlTemplate);
    }

    /// <summary>
    /// Returns the control template with the given name.
    /// </summary>
    public bool TryGetByName(string name, [NotNullWhen(true)] out ControlTemplate? controlTemplate)
    {
        return _controlTemplatesByName.TryGetValue(name, out controlTemplate);
    }

    /// <summary>
    /// Returns the control template with the given name.
    /// </summary>
    public ControlTemplate GetByName(string name)
    {
        return _controlTemplatesByName[name];
    }

    /// <summary>
    /// Returns the control template with the given uri.
    /// </summary>
    public bool TryGetByUri(string uri, [NotNullWhen(true)] out ControlTemplate? controlTemplate)
    {
        return _controlTemplatesByUri.TryGetValue(uri, out controlTemplate);
    }

    /// <summary>
    /// Returns the control template with the given uri.
    /// </summary>
    public ControlTemplate GetByUri(string name)
    {
        return _controlTemplatesByUri[name];
    }
}
