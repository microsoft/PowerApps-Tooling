// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
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
        public static string Version = "C5";

        public const string ChecksumName = "checksum.json";

        // Track a checksum per file and then merge into a single one at the end. 
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

        public static (string wholeChecksum, Dictionary<string, string> perFileChecksum) GetChecksum(string fullpathToMsApp)
        {
            using (var zip = ZipFile.OpenRead(fullpathToMsApp))
            {
                return GetChecksum(zip);
            }
        }

        public static (string wholeChecksum, Dictionary<string, string> perFileChecksum) GetChecksum(ZipArchive zip)
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
            var key = ChecksumFile<Sha256HashMaker>(filename, bytes);
            if (key != null)
            {
                _files.Add(filename, key);
            }
        }

        // Return null if not checksumed
        // T is the checksum aglorithm to use  - this allows passing in a debug algorithm. 
        internal static byte[] ChecksumFile<T>(string filename, byte[] bytes)
            where T : IHashMaker, new()
        {
            if (filename.EndsWith(ChecksumName, StringComparison.OrdinalIgnoreCase))
            {
                // Ignore the checksum file, else we'd have a circular reference. 
                return null;
            }

            if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith(".sarif", StringComparison.OrdinalIgnoreCase))
            {
                var key = ChecksumJsonFile<T>(filename, bytes);
                return key;
            }
            else
            {
                // $$$ hash this?
                return bytes;
            }
        }

        // These paths are json double-encoded and need a different comparer.
        private static HashSet<string> _jsonDouble = new HashSet<string>
        {
            "LocalConnectionReferences",
            "LocalDatabaseReferences",
            "LibraryDependencies",
            "DataSources\\TableDefinition",
            "DataSources\\WadlMetadata\\SwaggerJson",
        };

        // These paths are xml double-encoded and need a different comparer.
        private static HashSet<string> _xmlDouble = new HashSet<string>
        {
            "UsedTemplates\\Template",
            "DataSources\\WadlMetadata\\WadlXml",
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
                    if (this.s.Count >= 2)
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
                    if (this.s.Count >= 2)
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
        private static byte[] ChecksumJsonFile<T>(string filename, byte[] bytes)
            where T : IHashMaker, new()
        {
            var s = new ReadOnlyMemory<byte>(bytes);
            using (var doc = JsonDocument.Parse(s))
            {
                JsonElement je = doc.RootElement;

                var ctx = new Context { Filename = filename };

                using (var hash = new T())
                {
                    ChecksumJson(ctx, hash, je);

                    var key = hash.GetFinalValue();
                    return key;
                }
            }
        }

        internal static string ChecksumToString(byte[] bytes)
        {
            return Version + "_" + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Called after all files are added to get a checksum. 
        /// </summary>
        public (string wholeChecksum, Dictionary<string, string> perFileChecksum) GetChecksum()
        {
            var perFileChecksum = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var singleFileHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            foreach (var kv in _files.OrderBy(x => x.Key))
            {
                hash.AppendData(kv.Value);
                singleFileHash.AppendData(kv.Value);
                var singleFileHashResult = singleFileHash.GetHashAndReset();

                perFileChecksum.Add(kv.Key, ChecksumToString(singleFileHashResult));
            }

            var h = hash.GetHashAndReset();
            
            return (ChecksumToString(h), perFileChecksum);
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
        private static void ChecksumJson(Context ctx, IHashMaker hash, JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Array:
                    hash.AppendStartArray();
                    foreach (var element in je.EnumerateArray())
                    {
                        ChecksumJson(ctx, hash, element);
                    }
                    hash.AppendEndArray();
                    break;

                case JsonValueKind.Object:
                    hash.AppendStartObj();
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
                                hash.AppendPropNameSkip(prop.Name);

                                // Invariant script can contain Formulas. 
                                var str2 = prop.Value.GetString();
                                str2 = NormFormulaWhitespace(str2);
                                hash.AppendData(str2);
                            }
                            else if (kind != JsonValueKind.Null)
                            {
                                hash.AppendPropName(prop.Name);

                                ctx.Push(prop.Name);
                                ChecksumJson(ctx, hash, prop.Value);
                                ctx.Pop();
                            } else
                            {

                            }
                        }
                    }
                    hash.AppendEndObj();
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
                    hash.AppendNull();                    
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
                        var xmlString = parsedXML.ToString(SaveOptions.None).Replace("\r\n", "\n");
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
}
