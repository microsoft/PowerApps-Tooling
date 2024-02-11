// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

/// <summary>
/// Specifies that a object should be serialized to YAML.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[DebuggerDisplay("{Name}")]
public class YamlObjectAttribute : Attribute
{
    /// <summary>
    /// Name of the object in the YAML file.
    /// </summary>
    public string Name { get; set; }
}
