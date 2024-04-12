// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Extensions;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

/// <summary>
/// This class is meant to describe a Component definition.
/// </summary>
[FirstClass(templateName: BuiltInTemplates.Component)]
[YamlSerializable]
public record ComponentDefinition : Control, IConvertible
{
    protected ComponentDefinition() { }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [SetsRequiredMembers]
    public ComponentDefinition(string name, string variant, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Variant = variant;

        var baseTemplate = controlTemplateStore.GetByName(BuiltInTemplates.Component);
        Template = baseTemplate with { Name = baseTemplate.Name };
    }

    [YamlMember(Order = 98)]
    public string? Description { get; set; }

    [YamlMember(Order = 99)]
    public bool AccessAppScope { get; set; }

    [YamlMember(Order = 100)]
    public IList<CustomProperty> CustomProperties { get; set; } = new List<CustomProperty>();

    internal override void AfterCreate(Dictionary<string, object?> controlDefinition)
    {
        if (controlDefinition.TryGetValue<IList<CustomProperty>>(nameof(CustomProperties), out var customProperties))
        {
            if (customProperties != null)
                CustomProperties = customProperties;
        }

        if (controlDefinition.TryGetValue<string>(nameof(Description), out var description))
        {
            Description = description;
        }

        if (controlDefinition.TryGetValue<string>(nameof(AccessAppScope), out var tmpString) &&
            bool.TryParse(tmpString, out var accessAppScope))
        {
            AccessAppScope = accessAppScope;
        }
    }

    #region IConvertible

    public TypeCode GetTypeCode()
    {
        return TypeCode.Object;
    }

    public bool ToBoolean(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(bool)}");
    }

    public byte ToByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(byte)}");
    }

    public char ToChar(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(char)}");
    }

    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(DateTime)}");
    }

    public decimal ToDecimal(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(decimal)}");
    }

    public double ToDouble(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(double)}");
    }

    public short ToInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(short)}");
    }

    public int ToInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(int)}");
    }

    public long ToInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(long)}");
    }

    public sbyte ToSByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(sbyte)}");
    }

    public float ToSingle(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(float)}");
    }

    public string ToString(IFormatProvider? provider)
    {
        return Name;
    }

    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {conversionType.Name}");
    }

    public ushort ToUInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(ushort)}");
    }

    public uint ToUInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(uint)}");
    }

    public ulong ToUInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(ComponentDefinition)} to {typeof(ulong)}");
    }

    #endregion
}
