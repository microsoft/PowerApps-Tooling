// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;

namespace Microsoft.PowerPlatform.Formulas.Tools.Yaml;

/// <summary>
/// Serializer for Writing POCOs as canonical yaml. 
/// </summary>
public class YamlPocoSerializer : IDisposable
{
    YamlWriter _yamlWriter;
    private bool _isDisposed;

    public YamlPocoSerializer()
    {
    }

    public YamlPocoSerializer(YamlWriter yamlWriter)
    {
        _yamlWriter = yamlWriter ?? throw new ArgumentNullException(nameof(yamlWriter));
    }

    public YamlPocoSerializer(Stream stream)
    {
        _ = stream ?? throw new ArgumentNullException(nameof(stream));
        _yamlWriter = new YamlWriter(stream);
    }

    public static T Read<T>(TextReader reader)
    {
        var deserializer = new DeserializerBuilder().Build();

        // Throws if there are extra properties in Yaml 
        var obj = deserializer.Deserialize<T>(reader);
        return obj;
    }

    /// <summary>
    /// Write the object out to the stream in a canonical way. Such as:
    /// - object ordering
    /// - encodings
    /// - multi-line
    /// - newlines
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="obj"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void CanonicalWrite(TextWriter writer, object obj)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        _ = obj ?? throw new ArgumentNullException(nameof(obj));

        var yaml = new YamlWriter(writer);

        WriteObject(yaml, obj);
    }

    /// <summary>
    /// Serialize the object to the stream.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="name">Object name in Yaml</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Serialize(object obj, string name = null)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        _ = _yamlWriter ?? throw new InvalidOperationException("Writer is not set");

        // Write the object name.
        var objType = obj.GetType();
        var yamlObject = objType.GetCustomAttribute<YamlObjectAttribute>() ?? new YamlObjectAttribute() { Name = objType.Name };
        if (string.IsNullOrWhiteSpace(yamlObject.Name))
            yamlObject.Name = objType.Name;
        _yamlWriter.WriteStartObject(string.IsNullOrWhiteSpace(name) ? yamlObject.Name : name);

        // Get only the properties that have the YamlProperty attribute and non-default values.
        var yamlProps = new List<(PropertyInfo info, YamlPropertyAttribute attr)>();
        foreach (var prop in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(prop => Attribute.IsDefined(prop, typeof(YamlPropertyAttribute))))
        {
            var yamlProperty = prop.GetCustomAttribute<YamlPropertyAttribute>();
            var yamlPropertyValue = prop.GetValue(obj);
            if (yamlProperty.DefaultValue == null && yamlPropertyValue == null)
                continue;
            else if (yamlProperty.DefaultValue != null && yamlProperty.DefaultValue.Equals(yamlPropertyValue))
                continue;

            if (string.IsNullOrWhiteSpace(yamlProperty.Name))
                yamlProperty.Name = prop.Name;
            yamlProps.Add((prop, yamlProperty));
        }

        // Sort the properties by order, then by name.
        yamlProps.Sort((prop1, prop2) =>
        {
            return prop1.attr.CompareTo(prop2.attr);
        });

        // Write non-default sorted properties.
        var propsValues = new List<KeyValuePair<string, object>>();
        foreach (var prop in yamlProps)
        {
            WriteAnything(_yamlWriter, prop.attr.Name, prop.info.GetValue(obj));
        }
    }

    private static void WriteAnything(YamlWriter yaml, string propName, object obj)
    {
        if (obj == null)
        {
            // Exclude default values.
            // This provides more stable output as new things are added in the future.
            return;
        }
        else if (obj is bool valBool)
        {
            // Exclude default values.
            if (valBool)
            {
                yaml.WriteProperty(propName, valBool, false);
            }
        }
        else if (obj is string valString)
        {
            yaml.WriteProperty(propName, valString, false);
        }
        else if (obj is int valInt)
        {
            yaml.WriteProperty(propName, valInt, false);
        }
        else if (obj is double valDouble)
        {
            yaml.WriteProperty(propName, valDouble, false);
        }
        else if (obj.GetType().IsEnum)
        {
            var valEnum = obj.ToString();
            yaml.WriteProperty(propName, valEnum, false);
        }
        else
        {
            // Dictionary / nested object?
            if (obj is IDictionary dict)
            {
                if (dict.Count > 0)
                {
                    var list = new List<KeyValuePair<string, object>>();
                    foreach (DictionaryEntry kv in dict)
                    {
                        list.Add(new KeyValuePair<string, object>((string)kv.Key, kv.Value));
                    }


                    yaml.WriteStartObject(propName);
                    WriteCanonicalList(yaml, list);
                    yaml.WriteEndObject();
                }
            }
            else
            {
                yaml.WriteStartObject(propName);
                WriteObject(yaml, obj);
                yaml.WriteEndObject();
            }
        }
    }

    private static void WriteObject(YamlWriter yaml, object obj)
    {
        // Generic object.
        var outerType = obj.GetType();

        var list = new List<KeyValuePair<string, object>>();
        foreach (var prop in outerType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(x => x.Name))
        {
            var obj3 = prop.GetValue(obj);

            list.Add(new KeyValuePair<string, object>(prop.Name, obj3));
        } // loop properties

        WriteCanonicalList(yaml, list);
    }

    /// <summary>
    /// Write a list of properties (such as an object or dictionary) in an ordered way.
    /// </summary>
    /// <param name="yaml"></param>
    /// <param name="list"></param>
    private static void WriteCanonicalList(YamlWriter yaml, List<KeyValuePair<string, object>> list)
    {
        // Critical to sort to preserve a canonical order.
        list.Sort((kv1, kv2) => kv1.Key.CompareTo(kv2.Key));

        foreach (var kv in list)
        {
            WriteAnything(yaml, kv.Key, kv.Value);
        }
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _yamlWriter?.Dispose();
                _yamlWriter = null;
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
