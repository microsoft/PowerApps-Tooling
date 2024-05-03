// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "REVIEW: Consider fixing by renaming parameters or update this Justification.")]
public interface IControlFactory
{
    Control Create(string name, string template, string? componentDefinitionName = null, string? componentLibraryUniqueName = null, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    Control Create(string name, ControlTemplate template, string? componentDefinitionName = null, string? componentLibraryUniqueName = null, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    Control Create(string name, string template, string componentDefinitionName, string componentLibraryUniqueName, Dictionary<string, object?>? controlDefinition);

    App CreateApp(string name, ControlPropertiesCollection? properties = null);

    Screen CreateScreen(string name, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    ControlTemplate CreateComponentTemplate(string name, Dictionary<string, object?>? controlDefinition);

    ControlTemplate CreateComponentTemplate(string name, ComponentType componentType);
}
