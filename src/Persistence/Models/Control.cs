// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerPlatform.PowerApps.Persistence.Collections;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Models;

[DebuggerDisplay("{Template?.DisplayName}: {Name}")]
public abstract record Control : IConvertible
{
    public Control()
    {
    }

    [SetsRequiredMembers]
    public Control(string name, string variant, ControlTemplate template)
    {
        Name = name;
        Variant = variant;
        Template = template;
    }

    /// <summary>
    /// template uri of the control.
    /// </summary>
    [YamlMember(Alias = YamlFields.Control, Order = 0)]
    public string TemplateId => Template.Id;

    private string _name = string.Empty;
    /// <summary>
    /// the control's name.
    /// </summary>
    [YamlMember(Order = 10)]
    public required string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(Name));

            _name = value.Trim();
        }
    }

    [YamlMember(Order = 20)]
    public required string Variant { get; init; } = string.Empty;

    [YamlMember(Order = 30)]
    public string Layout { get; set; } = string.Empty;

    /// <summary>
    /// key/value pairs of Control properties. Mapped to/from Control rules.
    /// </summary>
    [YamlMember(Order = 40)]
    public ControlPropertiesCollection Properties { get; set; } = new();

    /// <summary>
    /// list of child controls nested under this control.
    /// This collection can be null in cases where the control does not support children.
    /// </summary>
    [YamlMember(Order = 50)]
    public IList<Control>? Children { get; set; }

    [YamlIgnore]
    public ControlEditorState? EditorState { get; set; }

    [YamlIgnore]
    public required ControlTemplate Template { get; init; }

    [YamlIgnore]
    public int ZIndex
    {
        get
        {
            if (Properties.TryGetValue(PropertyNames.ZIndex, out var prop) && int.TryParse(prop.Value, out var zIndex))
                return zIndex;

            return int.MaxValue;
        }
    }

    [YamlMember(Order = 60)]
    public string StyleName { get; set; } = string.Empty;

    internal virtual void AfterCreate(Dictionary<string, object?> controlDefinition)
    {
    }

    /// <summary>
    /// Called before serialization to hide nested templates which add properties to parent from YAML output.
    /// </summary>
    internal void HideNestedTemplates()
    {
        if (Children == null)
            return;

        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i].Template.AddPropertiesToParent)
            {
                foreach (var childTemplateProperty in Children[i].Properties)
                {
                    Properties.Add(childTemplateProperty.Key, childTemplateProperty.Value);
                }
                Children.RemoveAt(i);
                i--;
            }
        }
    }

    #region IConvertible

    public TypeCode GetTypeCode()
    {
        return TypeCode.Object;
    }

    public bool ToBoolean(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(bool)}");
    }

    public byte ToByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(byte)}");
    }

    public char ToChar(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(char)}");
    }

    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(DateTime)}");
    }

    public decimal ToDecimal(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(decimal)}");
    }

    public double ToDouble(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(double)}");
    }

    public short ToInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(short)}");
    }

    public int ToInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(int)}");
    }

    public long ToInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(long)}");
    }

    public sbyte ToSByte(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(sbyte)}");
    }

    public float ToSingle(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(float)}");
    }

    public string ToString(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(string)}");
    }

    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {conversionType.Name}");
    }

    public ushort ToUInt16(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(ushort)}");
    }

    public uint ToUInt32(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(uint)}");
    }

    public ulong ToUInt64(IFormatProvider? provider)
    {
        throw new NotSupportedException($"Cannot covert {GetType().Name} to {typeof(ulong)}");
    }

    #endregion
}
