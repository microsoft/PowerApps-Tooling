// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    /// <summary>
    /// Create a checksum over an .msapp file. 
    /// Must be tolerant to JSON files being non-canonical. 
    /// To aide in computing, allow checksum is computed out of order .
    /// </summary>
    public class ChecksumMaker
    {
        // Given checksum an easy prefix so that we can identify algorithm version changes. 
        public string Version = "C2";

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
            }
            else
            {
                _files.Add(filename, bytes);
            }
        }

        // These paths are json double-encoded and need a different comparer.
        private static HashSet<string> _jsonDouble = new HashSet<string>
        {
            "LocalConnectionReferences",
            "LocalDatabaseReferences",
            "DataSources\\TableDefinition"
        };

        // These paths are xml double-encoded and need a different comparer.
        private static HashSet<string> _xmlDouble = new HashSet<string>
        {
            "UsedTemplates\\Template",
        };

        // Helper for identifying which paths are double encoded. 
        // All of these should be resolved and fixed by the server. 
        private class Context
        {
            public string Filename;
            public Stack<string> s = new Stack<string>();

            public void Push(string path)
            {
                s.Push(path);
            }
            public void Pop()
            {
                s.Pop();
            }

            public bool IsJsonDoubleEncoded
            {
                get
                {
                    if (this.s.Count == 1)
                    {
                        if (_jsonDouble.Contains(this.s.Peek()))
                        {
                            return true;
                        }
                    }
                    if (this.s.Count == 2)
                    {
                        if (_jsonDouble.Contains(string.Join("\\", s.Reverse())))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            public bool IsXmlDoubleEncoded
            {
                get
                {
                    if (this.s.Count == 2)
                    {
                        if (_xmlDouble.Contains(string.Join("\\", s.Reverse())))
                        {
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
                    hash.AppendData(kv.Value);
                }

                var h = hash.GetHashAndReset();
                var str = Convert.ToBase64String(h);

                return Version + "_" + str;
            }
        }

        // Formula whitespace can differ between platforms, and leading whitespace
        // is affected by writing to .pa format. Normalize so checksums are
        // platform independent
        internal static string NormFormulaWhitespace(string s)
        {
            StringBuilder sb = new StringBuilder();
            var lastCharIsWhitespace = true;
            foreach (var ch in s)
            {
                bool isWhitespace = (ch == '\t') || (ch == ' ') || (ch == '\r') || (ch == '\n');
                if (isWhitespace)
                {
                    if (!lastCharIsWhitespace)
                    {
                        sb.Append(' ');
                    }
                    lastCharIsWhitespace = true;
                }
                else
                {
                    sb.Append(ch);
                    lastCharIsWhitespace = false;
                }
            }
            // Don't include trailing whitespace 
            while ((sb.Length > 1) && sb[sb.Length - 1] == ' ') { sb.Length--; }

            return sb.ToString();
        }

        // Traverse the Json object in a deterministic way
        private static void ChecksumJson(Context ctx, IncrementalHash hash, JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var element in je.EnumerateArray())
                    {
                        ChecksumJson(ctx, hash, element);
                    }
                    break;

                case JsonValueKind.Object:
                    // Server has bug where it double emits the same property!
                    // Only use once in checksum. 
                    HashSet<string> propertyNames = new HashSet<string>();

                    // Need to determinsitically order the properties. 
                    foreach (var prop in je.EnumerateObject().OrderBy(x => x.Name))
                    {
                        if (propertyNames.Add(prop.Name))
                        {
                            var kind = prop.Value.ValueKind;

                            if (kind == JsonValueKind.String && prop.Name == "InvariantScript")
                            {
                                // Invariant script can contain Formulas. 
                                var str2 = prop.Value.GetString();
                                str2 = NormFormulaWhitespace(str2);
                                hash.AppendData(str2);
                            }
                            else if (kind != JsonValueKind.Null)
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

                    if (ctx.IsJsonDoubleEncoded && !string.IsNullOrWhiteSpace(str))
                    {
                        var je2 = JsonDocument.Parse(str).RootElement;
                        ChecksumJson(ctx, hash, je2);
                    }
                    else if (ctx.IsXmlDoubleEncoded && !string.IsNullOrWhiteSpace(str))
                    {
                        var parsedXML = XDocument.Parse(str);
                        var xmlString = parsedXML.ToString(SaveOptions.None);
                        hash.AppendData(xmlString);
                    }
                    else
                    {
                        str = str.TrimStart().Replace("\r\n", "\n"); // Normalize line endings. 
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
