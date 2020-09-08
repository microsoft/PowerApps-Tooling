// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace PAModel
{
    /// <summary>
    /// Create a checksum over an .msapp file. 
    /// Must be tolerant to JSON files being non-canonical. 
    /// To aide in computing ... allow checksum is computed out of order .
    /// </summary>
    public class ChecksumMaker
    {
        // Given checksum an easy header so that we can identify algorithm version changes. 
        public string Version = "C1";

        public const string ChecksumName = "checksum.json";

        // Track a checksum per file and then merge into a single one at the end. 
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

        public static string GetChecksum(string fullpathToMsApp)
        {
            using (var zip = ZipFile.OpenRead(fullpathToMsApp))
            {
                return GetChecksum(zip);
            }
        }

        public static string GetChecksum(ZipArchive zip)
        {
            ChecksumMaker checksumMaker = new ChecksumMaker();

            foreach (var entry in zip.Entries)
            {
                checksumMaker.AddFile(entry.FullName, entry.ToBytes());
            }

            return checksumMaker.GetChecksum();
        }

        public void AddFile(string filename, byte[] bytes)
        {
#if false
            // Help in debugging checksum errors. 
            if (filename == "References\\DataSources.json")
            {
                var path = @"C:\temp\a1.json";
                var str = JsonNormalizer.Normalize(Encoding.UTF8.GetString(bytes));
                bytes = Encoding.UTF8.GetBytes(str);
                File.WriteAllBytes(path, bytes);
            }
#endif

            if (filename.EndsWith(ChecksumName, StringComparison.OrdinalIgnoreCase))
            {
                // Ignore the checksum file, else we'd have a circular reference. 
                return; 
            }
            if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith(".sarif", StringComparison.OrdinalIgnoreCase))
            {
                AddJsonFile(filename, bytes);
            } else
            {
                _files.Add(filename, bytes);
            }
        }

        // These paths are json double-encoded and need a different comparer.
        private static HashSet<string> _jsonDouble = new HashSet<string>
        {
            // "properties.json:"
            "LocalConnectionReferences",
            "LocalDatabaseReferences"
        };

        // Helper for identifying which paths are double encoded. 
        // All of these should be resolved and fixed by the server. 
        private class Context
        {
            public string Filename;
            public Stack<string> s = new Stack<string>();

            public void Push(string path)
            {
                this.s.Push(path);
            }
            public void Pop()
            {
                this.s.Pop();
            }

            public bool IsDoubleEncoded
            {
                get
                {
                    if (this.s.Count == 1)
                    {
                        if (_jsonDouble.Contains(this.s.Peek())) {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        // Add json - handle the non-canonical format. 
        private void AddJsonFile(string filename, byte[] bytes)
        {            
            var s = new ReadOnlyMemory<byte>(bytes);
            using (var doc = JsonDocument.Parse(s))
            {
                JsonElement je = doc.RootElement;

                var ctx = new Context { Filename = filename };

                using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                {
                    ChecksumJson(ctx, hash, je);

                    var key = hash.GetHashAndReset();
                    _files.Add(filename, key);
                }
            }
        }

        /// <summary>
        /// Called after all files are added to get a checksum. 
        /// </summary>
        public string GetChecksum()
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                foreach (var kv in _files.OrderBy(x => x.Key))
                {
                    //Console.WriteLine($"{kv.Key}: {Convert.ToBase64String(kv.Value)}");
                    hash.AppendData(kv.Value);
                }

                var h = hash.GetHashAndReset();
                var str = Convert.ToBase64String(h);

                //Console.WriteLine($"  Checksum: {str}");
                return Version + "_" + str;
            }
        }  
       
        // Traverse the Json object in a deterministic way
        private static void ChecksumJson(Context ctx, IncrementalHash hash, JsonElement je)
        {
            switch(je.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach(var element in je.EnumerateArray())
                    {
                        ChecksumJson(ctx, hash, element);
                    }
                    break;

                case JsonValueKind.Object:
                    // Server has bug where it double emits the same property!
                    // Only use once in checksum. 
                    HashSet<string> propertyNames = new HashSet<string>();

                    // Need to determinsitically order the properties. 
                    foreach (var prop in je.EnumerateObject().OrderBy(x=>x.Name))
                    {
                        if (propertyNames.Add(prop.Name))
                        {
                            var kind = prop.Value.ValueKind;
                            if (kind != JsonValueKind.Null && kind != JsonValueKind.False)
                            {
                                hash.AppendData(prop.Name);
                                ctx.Push(prop.Name);
                                ChecksumJson(ctx, hash, prop.Value);
                                ctx.Pop();
                            }
                        }
                    }
                    break;

                case JsonValueKind.Number:
                    hash.AppendData(je.GetDouble());
                    break;

                case JsonValueKind.False:
                    hash.AppendData(false);
                    break;

                case JsonValueKind.True:
                    hash.AppendData(true);
                    break;

                case JsonValueKind.Null:
                    hash.AppendData(false);
                    break;

                case JsonValueKind.String:
                    var str = je.GetString();

                    if (ctx.IsDoubleEncoded && !string.IsNullOrWhiteSpace(str))
                    {
                        var je2 = JsonDocument.Parse(str).RootElement;
                        ChecksumJson(ctx, hash, je2);
                    }
                    else
                    {
                        str = str.Replace("\r\n", "\n"); // Normalize line endings. 
                        hash.AppendData(str);
                    }
                    break;
            }
        }
    }

    static class IncrementalHashExtensions
    {
        public static void AppendData(this IncrementalHash hash, string x)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(x));
        }

        public static void AppendData(this IncrementalHash hash, double x)
        {
            hash.AppendData(BitConverter.GetBytes(x));
        }

        public static void AppendData(this IncrementalHash hash, bool x)
        {
            hash.AppendData(new byte[] { x ? (byte)1 : (byte)0 });
        }
    }
}
