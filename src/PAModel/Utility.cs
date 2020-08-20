using Microsoft.AppMagic.Persistence.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PAModel
{
    // Various utility methods. 
    internal static class Utility
    {
        public static IEnumerable<T> NullOk<T>(this IEnumerable<T> list)
        {
            if (list == null) return Enumerable.Empty<T>();
            return list;
        }
        public static TValue GetOrCreate<TKey,TValue>(this IDictionary<TKey, TValue> dict, TKey key)
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

        // System.IO.File's built in functions fail if the directory doesn't already exist. 
        // Must pre-create it before writing. 
        public static void EnsureFileDirExists(string path)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(path);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
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
            if (extra!= null && extra.Count > 0)
            {
                throw new NotSupportedException("There are fields in json we don't recognize");
            }
        }
    }
}
