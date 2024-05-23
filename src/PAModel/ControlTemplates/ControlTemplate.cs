// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.ControlTemplates;

internal sealed class ControlTemplate(string name, string version, string id)
{
    public string Name { get; } = name;
    public string Version { get; } = version;
    public string Id { get; } = id;

    // Property Name -> Default Expression
    public Dictionary<string, string> InputDefaults { get; } = new();

    // Variant name => property name => default expresion
    public Dictionary<string, Dictionary<string, string>> VariantDefaultValues { get; } = new();
}
