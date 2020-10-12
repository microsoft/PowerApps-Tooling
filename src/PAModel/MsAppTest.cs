// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class MsAppTest
    {
        // Given an msapp (original source of truth), stress test the conversions
        public static bool StressTest(string pathToMsApp)
        {
            using (var temp1 = new TempFile())
            {
                string outFile = temp1.FullPath;

                var log = TextWriter.Null;

                // MsApp --> Model
                CanvasDocument msapp;
                try
                {
                    msapp = MsAppSerializer.Load(pathToMsApp); // read
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine($"Too old: {pathToMsApp}");
                    return false;
                }

                // Model --> MsApp
                msapp.SaveAsMsApp(outFile);
                MsAppTest.Compare(pathToMsApp, outFile, log);


                // Model --> Source
                using (var tempDir = new TempDir())
                {
                    string outSrcDir = tempDir.Dir;
                    msapp.SaveAsSource(outSrcDir);

                    // Source --> Model
                    var msapp2 = SourceSerializer.LoadFromSource(outSrcDir);

                    msapp2.SaveAsMsApp(outFile); // Write out .pa files.
                    var ok = MsAppTest.Compare(pathToMsApp, outFile, log);
                    return ok;
                }
            }
        }

        public static bool Compare(string pathToZip1, string pathToZip2, TextWriter log)
        {
            var c1 = ChecksumMaker.GetChecksum(pathToZip1);
            var c2 = ChecksumMaker.GetChecksum(pathToZip2);
            if (c1 == c2)
            {
                return true;
            }

            // If there's a checksum mismatch, do a more intensive comparison to find the difference.

            // Provide a comparison that can be very specific about what the difference is.
            Dictionary<string, string> comp = new Dictionary<string, string>();
            var h1 = Test(pathToZip1, log, comp, true);
            var h2 = Test(pathToZip2, log, comp, false);


            foreach (var kv in comp) // Remaining entries are errors.
            {
                Console.WriteLine("FAIL: 2nd is missing " + kv.Key);
            }

            if (h1 == h2)
            {
                log.WriteLine("Same!");
                return true;
            }
            Console.WriteLine("FAIL!!");
            return false;
        }

        // Get a hash for the MsApp file.
        // First pass adds file/hash to comp.
        // Second pass checks hash equality and removes files from comp.
        // AFter second pass, comp should be 0. any files in comp were missing from 2nd pass.
        public static string Test(string pathToZip, TextWriter log, Dictionary<string,string> comp, bool first)
        {
            StringBuilder sb = new StringBuilder();

            log.WriteLine($">> {pathToZip}");
            using (var z = ZipFile.OpenRead(pathToZip))
            {
                foreach (ZipArchiveEntry e in z.Entries.OrderBy(x => x.FullName))
                {
                    if (e.Name.EndsWith(ChecksumMaker.ChecksumName))
                    {
                        continue;
                    }
                    string str;

                    // Compute a "smart" hash. Tolerant to whitespace in Json serialization.
                    if (e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var je = e.ToJson();
                        /*
                        str = JsonSerializer.Serialize(je, new JsonSerializerOptions
                        {
                            IgnoreNullValues = true,
                            WriteIndented = false,
                        });
                         str = je.ToString();
                         */

#if true
                        StringBuilder sb2 = new StringBuilder();
                        Check(sb2, je); ;
                        str = sb2.ToString();
#else
                        str = Hack1.Normalize(je);
#endif
                    }
                    else
                    {
                        var bytes = e.ToBytes();
                        str = Convert.ToBase64String(bytes);
                    }

                    // Do easy diffs
                    {
                        if (first)
                        {
                            comp.Add(e.FullName, str);
                        }
                        else
                        {
                            string otherContents;
                            if (comp.TryGetValue(e.FullName, out otherContents))
                            {
                                if (otherContents != str)
                                {
                                    // Fail! Mismatch
                                    Console.WriteLine("FAIL: hash mismatch: " + e.FullName);

                                    // Write out normalized form. Easier to spot the diff.
                                    File.WriteAllText(@"c:\temp\a1.json", otherContents);
                                    File.WriteAllText(@"c:\temp\b1.json", str);
                                }
                                else
                                {
                                    // success
                                }
                                comp.Remove(e.FullName);
                            }
                            else
                            {
                                // Missing file!
                                Console.WriteLine("FAIL: 2nd has added file: " + e.FullName);
                            }
                        }
                    }


                    var hash = str.GetHashCode().ToString();
                    log.WriteLine($"{e.FullName} ({hash})");

                    sb.Append($"{e.FullName},{hash};");
                }
            }
            log.WriteLine();

            return sb.ToString();
        }

        // Used for comparing equality of 2 json blobs.
        // Writing JsonElement is unordered. So do an ordered traversal.
        //   https://stackoverflow.com/questions/59134564/net-core-3-order-of-serialization-for-jsonpropertyname-system-text-json-seria
        static void Check(StringBuilder sb, JsonElement e, string indent = "")
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Array:
                    sb.Append(indent);
                    sb.AppendLine("[");

#if false
                    Dictionary<int, string> parts = new Dictionary<int, string>();

                    // Deterministic order for array output.
                    foreach (var x in e.EnumerateArray())
                    {
                        StringBuilder sb2 = new StringBuilder();
                        Check(sb2, x, indent +"  ");
                        var str = sb2.ToString();
                        parts[str.GetHashCode()] = str;
                    }

                    foreach(var kv in parts.OrderBy(kv2 => kv2.Key))
                    {
                        sb.AppendLine(kv.Value);
                    }
#else
                    foreach (var x in e.EnumerateArray())
                    {
                        Check(sb, x, indent + "  ");
                    }
#endif
                    sb.Append(indent);
                    sb.AppendLine("]");
                    break;
                case JsonValueKind.Object:
                    // We have a bug wehere we double emit the same field.
                    HashSet<string> dups = new HashSet<string>();

                    sb.Append(indent);
                    sb.AppendLine("{");
                    indent = indent + "  ";
                    foreach (var prop in e.EnumerateObject().OrderBy(x => x.Name))
                    {
                        if (prop.Name == "LocalConnectionReferences") {  }
                        if (dups.Add(prop.Name))
                        {
                            sb.Append(indent);
                            sb.Append(prop.Name);
                            Check(sb, prop.Value, indent + "  ");
                        } else
                        {

                        }
                    }
                    sb.Append(indent);
                    sb.AppendLine("}");

                    break;

                case JsonValueKind.String:
                    {
                        sb.Append(indent);

                        bool isDoubleEncodedJson = false;
                        var str = e.ToString();
                        if (str.Length >0)
                        {
                            if (str[0] == '{' && str[str.Length-1] == '}')
                            {
                                isDoubleEncodedJson = true;
                            }
                        }

                        if (isDoubleEncodedJson)
                        {
                            try
                            {
                                str = "<json>" + JsonNormalizer.Normalize(str) + "</json>";
                            }
                            catch { } // Not Json.
                        } else
                        {
                            str = e.ToString().TrimStart().Replace("\r\n", "\n").Replace("\r", "\n");
                        }

                        sb.AppendLine(str);                        
                    }
                    break;

                case JsonValueKind.Number:
                    // Normalize numbers. 3 and 3.0  should compare equals.
                    sb.Append(indent);
                    sb.AppendLine(e.GetDouble().ToString());
                    break;

                default:
                    sb.Append(indent);
                    sb.AppendLine(e.ToString().TrimStart().Replace("\r\n", "\n").Replace("\r", "\n"));
                    break;
            }
        }
    }
}
