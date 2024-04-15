// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// Represents an Canvas App.
/// </summary>
[FirstClass(templateName: "Appinfo")]
[YamlSerializable]
public record App : Control
{
    public App()
    {
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [SetsRequiredMembers]
    public App(string name, string variant, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Variant = variant;
        Template = controlTemplateStore.GetByName(BuiltInTemplates.App.Name);
    }

    [YamlIgnore]
    public IList<Screen> Screens { get; set; } = new List<Screen>();

    public Settings Settings { get; set; } = new Settings();

    internal override void AfterCreate(Dictionary<string, object?> controlDefinition)
    {
        if (controlDefinition.TryGetValue<Settings>(nameof(Settings), out var settings) && settings != null)
            Settings = settings;

        if (controlDefinition.TryGetValue<List<Screen>>(nameof(Screens), out var screens))
        {
            if (screens != null)
                Screens = screens;
            else
                Screens = new List<Screen>();
        }
    }
}
