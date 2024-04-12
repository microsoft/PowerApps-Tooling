// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public interface IControlFactory
{
    Control Create(string name, string template, string? componentDefinitionName = null, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    Control Create(string name, ControlTemplate template, string? componentDefinitionName = null, string? variant = null, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    Control Create(string name, string template, string componentDefinitionName, Dictionary<string, object?>? controlDefinition);

    App CreateApp(string name, ControlPropertiesCollection? properties = null);

    Screen CreateScreen(string name, ControlPropertiesCollection? properties = null, IList<Control>? children = null);

    ControlTemplate CreateControlTemplate(string name, Dictionary<string, object?>? controlDefinition);

    ControlTemplate CreateControlTemplate(string name, ComponentType componentType);
}
