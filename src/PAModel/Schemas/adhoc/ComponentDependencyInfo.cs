// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Schemas;

/// <summary>
/// Describes Properties.LibraryDependencies, which is an ordered json array of these.
/// Each item means a component was downloaded from a library. 
/// </summary>
internal class ComponentDependencyInfo
{
    // Matches against CombinedTemplateState.TemplateOriginalName
    public string OriginalComponentDefinitionTemplateId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
}
