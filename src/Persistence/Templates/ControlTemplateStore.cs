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

    public bool TryGetControlTemplateByName(string name, [NotNullWhen(true)] out ControlTemplate? controlTemplate)
    {
        return _controlTemplatesByName.TryGetValue(name, out controlTemplate);
    }

    public bool TryGetControlTemplateByUri(string uri, [NotNullWhen(true)] out ControlTemplate? controlTemplate)
    {
        return _controlTemplatesByUri.TryGetValue(uri, out controlTemplate);
    }
}
