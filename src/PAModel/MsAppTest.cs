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
            ErrorContainer errorContainer = new ErrorContainer();
            return Compare(pathToZip1, pathToZip2, log, errorContainer);
        }

        // Overload with ErrorContainer
        public static bool Compare(string pathToZip1, string pathToZip2, TextWriter log, ErrorContainer errorContainer)
        {
            var c1 = ChecksumMaker.GetChecksum(pathToZip1);
            var c2 = ChecksumMaker.GetChecksum(pathToZip2);
            if (c1.wholeChecksum == c2.wholeChecksum)
            {
                return true;
            }

            // Provide a comparison that can be very specific about what the difference is.
            var comp = new Dictionary<string, byte[]>();

            CompareChecksums(pathToZip1, log, comp, true, errorContainer);
            CompareChecksums(pathToZip2, log, comp, false, errorContainer);

            return false;
        }

        // Compare the debug checksums. 
        // Get a hash for the MsApp file.
        // First pass adds file/hash to comp.
        // Second pass checks hash equality and removes files from comp.
        // After second pass, comp should be 0. Any files in comp were missing from 2nd pass.
        public static void CompareChecksums(string pathToZip, TextWriter log, Dictionary<string, byte[]> comp, bool first, ErrorContainer errorContainer)
        {
            // Path to the directory where we are creating the normalized form
             string normFormDir = ".\\diffFiles";

            // Create directory if doesn't exist
            if (!Directory.Exists(normFormDir)) {
                Directory.CreateDirectory(normFormDir);
            }

            using (var zip = ZipFile.OpenRead(pathToZip))
            {
                foreach (ZipArchiveEntry entry in zip.Entries.OrderBy(x => x.FullName))
                {
                    var originalContents = ChecksumMaker.ChecksumFile<DebugTextHashMaker>(entry.FullName, entry.ToBytes());
                    if (originalContents == null)
                    {
                        continue;
                    }
                    
                    // Do easy diffs
                    {
                        if (first)
                        {
                            comp.Add(entry.FullName, originalContents);
                        }
                        else
                        {
                            byte[] newContents;
                            if (comp.TryGetValue(entry.FullName, out newContents))
                            {
                                bool same = originalContents.SequenceEqual(newContents);

                                if (!same)
                                {
                                    // Parse each byte array of the different files
                                    JsonElement json1 = JsonDocument.Parse(originalContents).RootElement;
                                    JsonElement json2 = JsonDocument.Parse(newContents).RootElement;

                                    // If mismatched, print name of top level object
                                    if (IsMismatched1(json1, json2, errorContainer))
                                    {
                                        errorContainer.JSONMismatch("Mismatched Property: " + currentProperty1.Name);
                                    }                                    

                                    // If mismatched, print name of top level object
                                    if (IsMismatched2(json1, json2, errorContainer))
                                    {
                                        errorContainer.JSONMismatch("Mismatched Property: " + currentProperty2.Name);
                                    }
                                

#if DEBUG
                                    DebugMismatch(entry, newContents, originalContents, normFormDir);
#endif
                                }

                                comp.Remove(entry.FullName);
                            }
                            else
                            {
                                // Missing file!
                                Console.WriteLine("FAIL: 2nd has added file: " + entry.FullName);
                            }
                        }
                    }
                }
            }

        }

  

        public static bool IsMismatched1(JsonElement json1, JsonElement json2, ErrorContainer errorContainer)
        {
            // Check each property and value in json1 to see if each exists and is equal to json2
            foreach (var currentProperty in json1.EnumerateObject())
            {
                // If an array
                if (currentProperty.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var subproperty in currentProperty.Value.EnumerateArray())
                    {
                        IsMismatched1(json1, json2, errorContainer);
                    }
                }

                // If current property from first json file also exists in the second file
                if (json2.TryGetProperty(currentProperty.Name, out JsonElement value2))
                {
                    // If current property value from first json file is not the same as in second
                    if (!currentProperty.Value.Equals(value2))
                    {
                        return true;
                    }
                }
                // If current property from first file does not exist in second
                else
                {
                    return true;
                }
            }
            return false;
        }



        public static bool IsMismatched2(JsonElement json1, JsonElement json2, ErrorContainer errorContainer)
        {
            // If an array
            if (currentProperty.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var subproperty in currentProperty.Value.EnumerateArray())
                {
                    IsMismatched2(json1, json2, errorContainer);
                }
            }

            // If current property from second json file does not exist in the first file
            if (!json1.TryGetProperty(currentProperty.Name, out JsonElement value1))
            {
                return true;
            }
            
            return false;
        }

        public static void DebugMismatch(ZipArchiveEntry entry, byte[] newContents, byte[] originalContents, string normFormDir)
        {
            // Fail! Mismatch
            Console.WriteLine("FAIL: hash mismatch: " + entry.FullName);

            // Paths to current diff files
            string aPath = normFormDir + "\\" + Path.ChangeExtension(entry.Name, null) + "-A.json";
            string bPath = normFormDir + "\\" + Path.ChangeExtension(entry.Name, null) + "-B.json";

            File.WriteAllBytes(aPath, newContents);
            File.WriteAllBytes(bPath, originalContents);
        }
    }
}
