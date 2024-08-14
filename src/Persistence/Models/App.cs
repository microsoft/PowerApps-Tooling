// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
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
    // App control has a special name.
    public const string ControlName = "App";

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

    // [YamlMember(Alias = "Datasources")]
    public DataSourcesMap DataSources { get; set; }// = new List<DataSource>();

    internal override void AfterCreate(Dictionary<string, object?> controlDefinition)
    {
        if (controlDefinition.TryGetValue<List<Screen>>(nameof(Screens), out var screens))
        {
            if (screens != null)
                Screens = screens;
            else
                Screens = new List<Screen>();
        }

        if (controlDefinition.TryGetValue<DataSourcesMap>(nameof(DataSources), out var datasources))
        {
            if (datasources != null)
                DataSources = datasources;
        }
    }

  //  public DataSources1 Datasources { get; set; }
}

//public record DataSources1
//{
//    public string? Type { get; set; }
//}
