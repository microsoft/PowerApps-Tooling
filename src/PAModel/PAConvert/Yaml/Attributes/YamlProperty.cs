// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

/// <summary>
/// Specifies that a property should be serialized to YAML.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
[DebuggerDisplay("{Name}")]
public class YamlPropertyAttribute : Attribute, IComparable<YamlPropertyAttribute>
{
    /// <summary>
    /// Name of the property in the YAML file.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Order in which the property should be serialized.
    /// If not specified, the property will be serialized last.
    /// </summary>
    public int Order { get; init; } = int.MaxValue;

    /// <summary>
    /// Default value of the property.
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// Compare the order of this property to another.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(YamlPropertyAttribute other)
    {
        if (other == null)
            return 1;

        if (Order != other.Order)
            return Order.CompareTo(other.Order);

        if (Name == null)
            return -1;

        return Name.CompareTo(other.Name);
    }
}
