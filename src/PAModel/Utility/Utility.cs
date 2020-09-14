// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Persistence.Converters;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("PAModelTests")]
[assembly: InternalsVisibleTo("PASopa")]
namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Various utility methods. 
    internal static class Utility
    {
        public static IEnumerable<T> NullOk<T>(this IEnumerable<T> list)
        {
            if (list == null) return Enumerable.Empty<T>();
            return list;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default)
            where TValue : new()
        {
            TValue value;
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }
            return @default;
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            TValue value;
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }
            value = new TValue();
            dict[key] = value;
            return value;
        }

        static JsonSerializerOptions GetJsonOptions()
        {
            var opts = new JsonSerializerOptions();

            // encodes quote as \" rather than unicode. 
            opts.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            opts.Converters.Add(new JsonStringEnumConverter());

            opts.Converters.Add(new JsonDateTimeConverter());
            opts.Converters.Add(new JsonVersionConverter());

            opts.WriteIndented = true;
            opts.IgnoreNullValues = true;

            return opts;
        }

        public static JsonSerializerOptions _jsonOpts = GetJsonOptions();

        // https://stackoverflow.com/questions/58138793/system-text-json-jsonelement-toobject-workaround

        public static string JsonSerialize<T>(T obj)
        {
            return JsonSerializer.Serialize<T>(obj, _jsonOpts);
        }

        public static T JsonParse<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }

        public static T ToObject<T>(this JsonElement element)
        {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }
        public static T ToObject<T>(this JsonDocument document)
        {
            var json = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }

        public static byte[] ToBytes(this JsonElement e)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(e, _jsonOpts);
            return bytes;
        }


        public static byte[] ToBytes(this ZipArchiveEntry e)
        {
            using (var s = e.Open())
            {
                // Non-seekable stream. 
                var len = e.Length;
                var buffer = new byte[len];
                s.Read(buffer, 0, (int)len);
                return buffer;
            }
        }

        // JsonElement is loss-less, handles unknown fields without dropping them. 
        // Convertering to a Poco drops fields we don't recognize. 
        public static JsonElement ToJson(this ZipArchiveEntry e)
        {
            using (var s = e.Open())
            {
                var doc = JsonDocument.Parse(s);
                return doc.RootElement;
            }
        }

        // full:     C:\foo
        // basePath: c:\foo\bar\hi.txt
        // returns "bar\hi.txt"
        public static string GetRelativePath(string full, string basePath)
        {
            // full is a prefix of basePath. 
            return full.Substring(basePath.Length + 1);
        }

        public static void EnsureNoExtraData(Dictionary<string, JsonElement> extra)
        {
            if (extra != null && extra.Count > 0)
            {
                throw new NotSupportedException("There are fields in json we don't recognize");
            }
        }

        // Useful when we need to mutate an object (such as adding back properties) 
        // but don't want to mutate the original.
        public static T JsonClone<T>(this T obj)
        {
            var str = JsonSerializer.Serialize(obj, _jsonOpts);
            var obj2 = JsonSerializer.Deserialize<T>(str, _jsonOpts);
            return obj2;
        }

        private const char EscapeChar = '%';

        private static bool DontEscapeChar(char ch)
        {
            // List of "safe" characters that aren't escaped. 
            // Note that the EscapeChar must be escaped, so can't appear on this list! 
            return
                (ch >= '0' && ch <= '9') ||
                (ch >= 'a' && ch <= 'z') ||
                (ch >= 'A' && ch <= 'Z') ||
                (ch == '[' || ch == ']') || // common in SQL connection names 
                (ch == '_') ||
                (ch == '.') ||
                (ch == ' '); // allow spaces, very common.
        }

        // For writing out to a director. 
        public static string EscapeFilename(string path)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ch in path)
            {
                if (DontEscapeChar(ch))
                {
                    sb.Append(ch);
                }
                else
                {
                    var x = (int)ch;
                    if (x <= 255)
                    {
                        sb.Append(EscapeChar);
                        sb.Append(x.ToString("x2"));
                    }
                    else
                    {
                        sb.Append(EscapeChar);
                        sb.Append(EscapeChar);
                        sb.Append(x.ToString("x4"));
                    }
                }
            }
            return sb.ToString();
        }

        private static int ToHex(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                return ((int)ch) - '0';
            }
            if (ch >= 'a' && ch <= 'f')
            {
                return (((int)ch) - 'a') + 10;
            }
            if (ch >= 'A' && ch <= 'F')
            {
                return (((int)ch) - 'A') + 10;
            }
            throw new InvalidOperationException($"Unrecognized hex char {ch}");
        }
        public static string UnEscapeFilename(string path)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < path.Length; i++)
            {
                var ch = path[i];
                if (DontEscapeChar(ch))
                {
                    sb.Append(ch);
                }
                else if (ch == EscapeChar)
                {
                    // Unescape 
                    int x;
                    if (path[i + 1] == EscapeChar)
                    {
                        i++;
                        x = ToHex(path[i + 1]) * 16 * 16 * 16 +
                            ToHex(path[i + 2]) * 16 * 16 +
                            ToHex(path[i + 3]) * 16 +
                            ToHex(path[i + 4]);
                        i += 4;
                    }
                    else
                    {
                        // 2 digit
                        x = ToHex(path[i + 1]) * 16 +
                            ToHex(path[i + 2]);
                        i += 2;
                    }
                    sb.Append((char)x);
                }
                else
                {
                    // Error 
                    throw new InvalidOperationException($"Can't unescape path: {path}");
                }
            }
            return sb.ToString();
        }
    }
}
