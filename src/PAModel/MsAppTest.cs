// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.MergeTool;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
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
        public static bool Compare(CanvasDocument doc1, CanvasDocument doc2, TextWriter log)
        {
            using (var temp1 = new TempFile())
            using (var temp2 = new TempFile())
            {
                doc1.SaveToMsApp(temp1.FullPath);
                doc2.SaveToMsApp(temp2.FullPath);
                return Compare(temp1.FullPath, temp2.FullPath, log);
            }
        }

        public static bool MergeStressTest(string pathToMsApp1, string pathToMsApp2)
        {
            try
            {
                (CanvasDocument doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp1);
                errors.ThrowOnErrors();

                (var doc2, var errors2) = CanvasDocument.LoadFromMsapp(pathToMsApp2);
                errors2.ThrowOnErrors();

                var doc1New = CanvasMerger.Merge(doc1, doc2, doc2);
                var ok1 = HasNoDeltas(doc1, doc1New);

                var doc2New = CanvasMerger.Merge(doc2, doc1, doc1);
                var ok2 = HasNoDeltas(doc2, doc2New);

                return ok1 && ok2;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public static bool TestClone(string pathToMsApp)
        {
            (CanvasDocument doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
            errors.ThrowOnErrors();

            var docClone = new CanvasDocument(doc1); 

            return HasNoDeltas(doc1, docClone, strict: true);
        }


        public static bool DiffStressTest(string pathToMsApp)
        {
            (CanvasDocument doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
            errors.ThrowOnErrors();

            return HasNoDeltas(doc1, doc1);
        }

        // Verify there are no deltas (detected via smart merge) between doc1 and doc2
        // Strict =true, also compare entropy files. 
        private static bool HasNoDeltas(CanvasDocument doc1, CanvasDocument doc2, bool strict = false)
        {
            var ourDeltas = Diff.ComputeDelta(doc1, doc1);

            // ThemeDelta always added
            ourDeltas = ourDeltas.Where(x => x.GetType() != typeof(ThemeChange)).ToArray();

            if (ourDeltas.Any())
            {
                foreach (var diff in ourDeltas)
                {
                    Console.WriteLine($"  {diff.GetType().Name}");
                }
                // Error! app shouldn't have any diffs with itself.
                return false;
            }


            // Save and verify checksums.
            using (var temp1 = new TempFile())
            using (var temp2 = new TempFile())
            {
                doc1.SaveToMsApp(temp1.FullPath);
                doc2.SaveToMsApp(temp2.FullPath);

                bool same;
                if (strict)
                {
                    same = Compare(temp1.FullPath, temp2.FullPath, Console.Out);
                }
                else
                {
                    var doc1NoEntropy = RemoveEntropy(temp1.FullPath);
                    var doc2NoEntropy = RemoveEntropy(temp2.FullPath);

                    same = Compare(doc1NoEntropy, doc2NoEntropy, Console.Out);
                }

                if (!same)
                {
                    return false;
                }
            }

            return true;
        }

        // Unpack, delete the entropy dirs, repack. 
        public static CanvasDocument RemoveEntropy(string pathToMsApp)
        {
            using (var temp1 = new TempDir())
            {
                (CanvasDocument doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
                errors.ThrowOnErrors();

                doc1.SaveToSources(temp1.Dir);

                var entropyDir = Path.Combine(temp1.Dir, "Entropy");
                if (!Directory.Exists(entropyDir))
                {
                    throw new Exception($"Missing entropy dir: " + entropyDir);
                }

                Directory.Delete(entropyDir, recursive: true);

                (var doc2, var errors2) = CanvasDocument.LoadFromSources(temp1.Dir);
                errors.ThrowOnErrors();

                return doc2;
            }
        }


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
                        using (var stream = new FileStream(pathToMsApp, FileMode.Open))
                        {
                            msapp = MsAppSerializer.Load(stream, errors);
                        }
                        errors.Write(log);
                        errors.ThrowOnErrors();

                        // We can still get warnings here. Commonly:
                        // - PA2001, checksum mismatch
                        // - PA2999, colliding asset names
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
                        errors = msapp.SaveToSources(outSrcDir, verifyOriginalPath : pathToMsApp);
                        errors.ThrowOnErrors();                 
                    }
                } // end using

                if (!MsAppTest.TestClone(pathToMsApp))
                {
                    return false;
                }

                if (!MsAppTest.DiffStressTest(pathToMsApp))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            

            return true;
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
            var comp = new Dictionary<string, byte[]>();
            DebugChecksum(pathToZip1, log, comp, true);
            DebugChecksum(pathToZip2, log, comp, false);

            foreach (var kv in comp) // Remaining entries are errors.
            {
                Console.WriteLine("FAIL: 2nd is missing " + kv.Key);
            }
#endif
            return false;
        }

        // Compare the debug checksums. 
        // Get a hash for the MsApp file.
        // First pass adds file/hash to comp.
        // Second pass checks hash equality and removes files from comp.
        // AFter second pass, comp should be 0. any files in comp were missing from 2nd pass.
        public static void DebugChecksum(string pathToZip, TextWriter log, Dictionary<string, byte[]> comp, bool first)
        {
            // Path to the directory where we are creating the normalized form
             string normFormDir = ".\\diffFiles";

            // Create directory if doesn't exist
            if (!Directory.Exists(normFormDir)) {
                Directory.CreateDirectory(normFormDir);
            }

            using (var z = ZipFile.OpenRead(pathToZip))
            {
                foreach (ZipArchiveEntry e in z.Entries.OrderBy(x => x.FullName))
                {
                    var key = ChecksumMaker.ChecksumFile<DebugTextHashMaker>(e.FullName, e.ToBytes());
                    if (key == null)
                    {
                        continue;
                    }
                    
                    // Do easy diffs
                    {
                        if (first)
                        {
                            comp.Add(e.FullName, key);
                        }
                        else
                        {
                            byte[] otherContents;
                            if (comp.TryGetValue(e.FullName, out otherContents))
                            {

                                bool same = key.SequenceEqual(otherContents);

                                if (!same)
                                {
                                    // Fail! Mismatch
                                    Console.WriteLine("FAIL: hash mismatch: " + e.FullName);
    
                                    // Paths to current diff files
                                    string aPath = normFormDir + "\\" + Path.ChangeExtension(e.Name, null) + "-A.json";
                                    string bPath = normFormDir + "\\" + Path.ChangeExtension(e.Name, null) + "-B.json";

                                    File.WriteAllBytes(aPath, otherContents);
                                    File.WriteAllBytes(bPath, key);

                                    // For debugging. Help find exactly where the difference is. 
                                    for (int i = 0; i < otherContents.Length; i++)
                                    {
                                        if (i >= key.Length)
                                        {
                                            break;
                                        }
                                        if (otherContents[i] != key[i])
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
                }
            }
        }
    }
}
