// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// Represents an Canvas App.
/// </summary>
[FirstClass(templateName: BuiltInTemplates.App)]
[YamlSerializable]
public record App : Control
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [SetsRequiredMembers]
    public App(string name, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Template = controlTemplateStore.GetByName(BuiltInTemplates.App);
    }

    [YamlIgnore]
    public IList<Screen> Screens { get; set; } = new List<Screen>();
}
