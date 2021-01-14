// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Microsoft.PowerPlatform.Formulas.Tools
{
    internal class MsAppTest
    {
        // Given an msapp (original source of truth), stress test the conversions
        public static bool StressTest(string pathToMsApp)
        {
            try
            {
                using (var temp1 = new TempFile())
                {
                    string outFile = temp1.FullPath;

                    var log = TextWriter.Null;

                    // MsApp --> Model
                    CanvasDocument msapp;
                    ErrorContainer errors = new ErrorContainer();
                    try
                    {
                        msapp = MsAppSerializer.Load(pathToMsApp, errors); // read
                        errors.ThrowOnErrors();
                    }
                    catch (NotSupportedException)
                    {
                        errors.FormatNotSupported($"Too old: {pathToMsApp}");
                        return false;
                    }

                    // Model --> MsApp
                    errors = msapp.SaveToMsApp(outFile);
                    errors.ThrowOnErrors();
                    var ok = MsAppTest.Compare(pathToMsApp, outFile, log);
                    if (!ok) { return false; }


                    // Model --> Source
                    using (var tempDir = new TempDir())
                    {
                        string outSrcDir = tempDir.Dir;
                        errors = msapp.SaveToSources(outSrcDir);
                        errors.ThrowOnErrors();

                        // Source --> Model
                        (var msapp2, var errors2) = CanvasDocument.LoadFromSources(outSrcDir);
                        errors2.ThrowOnErrors();

                        errors2 = msapp2.SaveToMsApp(outFile); // Write out .pa files.
                        errors2.ThrowOnErrors();
                        var ok2 = MsAppTest.Compare(pathToMsApp, outFile, log);
                        return ok2;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public static bool Compare(string pathToZip1, string pathToZip2, TextWriter log)
        {
            var c1 = ChecksumMaker.GetChecksum(pathToZip1);
            var c2 = ChecksumMaker.GetChecksum(pathToZip2);
            if (c1.wholeChecksum == c2.wholeChecksum)
            {
                return true;
            }

            // If there's a checksum mismatch, do a more intensive comparison to find the difference.
#if DEBUG
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
#endif
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
                        str = JsonNormalizer.Normalize(je);
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

                                    // For debugging. Help find exactly where the difference is. 
                                    for (int i = 0; i < otherContents.Length; i++)
                                    {
                                        if (i >= str.Length)
                                        {
                                            break;
                                        }
                                        if (otherContents[i] != str[i])
                                        {

                                        }
                                    }
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
    }
}
