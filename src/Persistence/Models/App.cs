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
[FirstClass(templateName: BuiltInTemplates.App)]
[YamlSerializable]
public record App : Control, IConvertible
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
        Template = controlTemplateStore.GetByName(BuiltInTemplates.App);
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

    #region IConvertible

    TypeCode IConvertible.GetTypeCode()
    {
        return TypeCode.Object;
    }

    bool IConvertible.ToBoolean(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(bool)}");
    }

    byte IConvertible.ToByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(byte)}");
    }

    char IConvertible.ToChar(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(char)}");
    }

    DateTime IConvertible.ToDateTime(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(DateTime)}");
    }

    decimal IConvertible.ToDecimal(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(decimal)}");
    }

    double IConvertible.ToDouble(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(double)}");
    }

    short IConvertible.ToInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(short)}");
    }

    int IConvertible.ToInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(int)}");
    }

    long IConvertible.ToInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(long)}");
    }

    sbyte IConvertible.ToSByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(sbyte)}");
    }

    float IConvertible.ToSingle(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(float)}");
    }

    string IConvertible.ToString(IFormatProvider? provider)
    {
        return Name;
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {conversionType.Name}");
    }

    ushort IConvertible.ToUInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(ushort)}");
    }

    uint IConvertible.ToUInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(uint)}");
    }

    ulong IConvertible.ToUInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {typeof(App)} to {typeof(ulong)}");
    }

    #endregion
}
