using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PAModel
{
    public class MsAppTest
    {
        // Given an msapp (original source of truth), stress test the conversions
        public static bool StressTest(string pathToMsApp)
        {
            string outFile = Path.GetTempFileName() + ".msapp";

            var log = TextWriter.Null;

            // MsApp --> Model
            var msapp = MsAppSerializer.Load(pathToMsApp); // read 

            // Model --> MsApp
            msapp.SaveAsMsApp(outFile);
            MsAppTest.Compare(pathToMsApp, outFile, log);


            // Model --> Source 
            string outSrcDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            msapp.SaveAsSource(outSrcDir);

            // Source --> Model
            var msapp2 = SourceSerializer.LoadFromSource(outSrcDir);

            msapp2.SaveAsMsApp(outFile); // Write out .pa files. 
            var ok = MsAppTest.Compare(pathToMsApp, outFile, log);
            return ok;
        }

        public static bool Compare(string pathToZip1, string pathToZip2, TextWriter log)
        {
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


                        StringBuilder sb2 = new StringBuilder();
                        Check(sb2, je); ;
                        str = sb2.ToString();
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
                                Console.WriteLine("FAIL: 1st is missing " + e.FullName);
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

                    sb.Append(indent);
                    sb.AppendLine("]");
                    break;
                case JsonValueKind.Object:
                    // We have a bug wehere we double emit the same field. 
                    HashSet<string> dups = new HashSet<string>();
                    foreach (var prop in e.EnumerateObject().OrderBy(x => x.Name))
                    {
                        if (dups.Add(prop.Name))
                        {
                            sb.Append(indent);
                            sb.AppendLine("{");

                            sb.Append(indent);
                            sb.Append(prop.Name);
                            Check(sb, prop.Value, indent + "  ");

                            sb.Append(indent);
                            sb.AppendLine("}");
                        } else
                        {

                        }
                    }
                    break;

                default:
                    sb.Append(indent);
                    sb.AppendLine(e.ToString());
                    break;
            }
        }
    }
}