// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Attributes;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[FirstClass(templateName: BuiltInTemplates.Screen)]
[YamlSerializable]
public record Screen : Control, IConvertible
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [SetsRequiredMembers]
    public Screen(string name, string variant, IControlTemplateStore controlTemplateStore)
    {
        Name = name;
        Variant = variant;
        Template = controlTemplateStore.GetByName(BuiltInTemplates.Screen);
    }

    public TypeCode GetTypeCode()
    {
        return TypeCode.Object;
    }

    public bool ToBoolean(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(bool)}");
    }

    public byte ToByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(byte)}");
    }

    public char ToChar(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(char)}");
    }

    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(DateTime)}");
    }

    public decimal ToDecimal(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(decimal)}");
    }

    public double ToDouble(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(double)}");
    }

    public short ToInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(short)}");
    }

    public int ToInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(int)}");
    }

    public long ToInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(long)}");
    }

    public sbyte ToSByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(sbyte)}");
    }

    public float ToSingle(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(float)}");
    }

    public string ToString(IFormatProvider? provider)
    {
        return Name;
    }

    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {conversionType.Name}");
    }

    public ushort ToUInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(ushort)}");
    }

    public uint ToUInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(uint)}");
    }

    public ulong ToUInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(Screen)} to {typeof(ulong)}");
    }
}
