// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "REVIEW: Consider fixing by renaming parameters or update this Justification.")]
public interface IControlFactory
{
    Control Create(string name, string templateNameOrId);

    Control Create(string name, string templateNameOrId, bool isClassic = false, string? componentDefinitionName = null, string? componentLibraryUniqueName = null, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    Control Create(string name, string templateId, string? componentDefinitionName = null, string? componentLibraryUniqueName = null, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    Control Create(string name, ControlTemplate template, string? componentDefinitionName = null, string? componentLibraryUniqueName = null, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    Control Create(string name, string template, bool isClassic = false, string? componentDefinitionName = null, string? componentLibraryUniqueName = null, Dictionary<string, object?>? controlDefinition = null);

    App CreateApp(string name, ControlPropertiesCollection? properties = null);

    Screen CreateScreen(string name, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    ControlTemplate CreateControlTemplate(string name, Dictionary<string, object?>? controlDefinition);

    ControlTemplate CreateControlTemplate(string name, ComponentType componentType);
}
