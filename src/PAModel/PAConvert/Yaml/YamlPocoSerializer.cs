// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerPlatform.Formulas.Tools.PAConvert.Yaml
{
    /// <summary>
    /// Serializer for Writing Pocos as canonical yaml. 
    /// </summary>
    public static class YamlPocoSerializer
    {
        public static T Read<T>(TextReader reader)
            where T : new()
        {
            var deserializer = new DeserializerBuilder().Build();

            // Throws if there are extra properties in Yaml 
            var obj = deserializer.Deserialize<T>(reader);
            return obj;
        }

        // Write the object out to the stream in a canonical way. Such as:
        // - object ordering
        // - encodings
        // - multi-line
        // - newlines
        public static void CanonicalWrite(TextWriter writer, object obj)
        {
            var yaml = new YamlWriter(writer);

            WriteObject(yaml, obj);
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
                    yaml.WriteProperty(propName, valBool);
                }
            }
            else if (obj is string valString)
            {
                yaml.WriteProperty(propName, valString, false);
            }
            else if (obj is int valInt)
            {
                yaml.WriteProperty(propName, valInt);
            }
            else if (obj is double valDouble)
            {
                yaml.WriteProperty(propName, valDouble);
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
                            list.Add(new KeyValuePair<string, object>((string) kv.Key, kv.Value));
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

        // Write a list of properties (such as an object or dictionary) in an ordered way.
        private static void WriteCanonicalList(YamlWriter yaml, List<KeyValuePair<string, object>> list)
        {
            // Critical to sort to preserve a canonical order.
            list.Sort((kv1, kv2) => kv1.Key.CompareTo(kv2.Key));

            foreach(var kv in list)
            {
                WriteAnything(yaml, kv.Key, kv.Value);
            }
        }
    }
}
