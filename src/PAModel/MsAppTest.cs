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
                    var newContents = ChecksumMaker.ChecksumFile<DebugTextHashMaker>(entry.FullName, entry.ToBytes());
                    if (newContents == null)
                    {
                        continue;
                    }
                    
                    // Do easy diffs
                    {
                        if (first)
                        {
                            comp.Add(entry.FullName, newContents);
                        }
                        else
                        {
                            byte[] originalContents;
                            if (comp.TryGetValue(entry.FullName, out originalContents))
                            {
                                bool same = newContents.SequenceEqual(originalContents);

                                if (!same)
                                { 
                                    var jsonDictionary1 = FlattenJson(originalContents);
                                    var jsonDictionary2 = FlattenJson(newContents);

                                    // Add JSONMismatch error if JSON property was changed or removed
                                    CheckPropertyChangedRemoved(jsonDictionary1, jsonDictionary2, errorContainer, "");

                                    // Add JSONMismatch error if JSON property was added
                                    CheckPropertyAdded(jsonDictionary1, jsonDictionary2, errorContainer, "");
#if DEBUG
                                    DebugMismatch(entry, originalContents, newContents, normFormDir);
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

        public static Dictionary<string, JsonElement> FlattenJson(byte[] json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                var jsonObject = document.RootElement.EnumerateObject().SelectMany(property => GetLeaves(null, property));
                return jsonObject.ToDictionary(key => key.Path, value => value.Property.Value.Clone());
            }
                
        }

        public static IEnumerable<(string Path, JsonProperty Property)> GetLeaves(string path, JsonProperty property)
        {
            if (path == null)
            {
                path = property.Name;
            }
            else
            {
                path += "." + property.Name;
            }

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                return property.Value.EnumerateObject().SelectMany(child => GetLeaves(path, child));
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                if (property.Value.GetArrayLength() == 0)
                {
                    return new[] { (path, property) };
                }
                else
                {
                    var arrayType = property.Value[0].ValueKind;

                    // Peek, if member types, return
                    if (arrayType == JsonValueKind.Object)
                    {
                        return FlattenArray(path, property.Value);
                    }
                    else
                    {
                        return new[] { (path, property) };
                    }
                }
            }
            else
            {
                return new[] { (path, property) };
            }
        }
        public static IEnumerable<(string Path, JsonProperty Property)> FlattenArray(string path, JsonElement array)
        {
            List<(string arrayPath, JsonProperty arrayProperty)> enumeratedObjects = new List<(string arrayPath, JsonProperty arrayProperty)>();
            
            int index = 0;

            foreach (var member in array.EnumerateArray())
            {
                string arraySubPath = $"{path}[{index}]";

                if (member.ValueKind == JsonValueKind.Object)
                {
                    enumeratedObjects.AddRange(member.EnumerateObject().SelectMany(child => GetLeaves(arraySubPath, child)));
                }

                index++;
            }
            return enumeratedObjects.ToArray();
        }

        public static void CheckPropertyChangedRemoved(Dictionary<string, JsonElement> dictionary1, Dictionary<string, JsonElement> dictionary2, ErrorContainer errorContainer, string jsonPath)
        {
            // Iterate through each path/json pair in Dictionary 1
            foreach (var currentPair1 in dictionary1)
            {
                // Check if the second dictionary contains the same key as in Dictionary 1
                if (dictionary2.TryGetValue(currentPair1.Key, out JsonElement json2))
                {
                    // Check if the value in Dictionary 2's property is equal to the value in Dictionary1's property
                    if (!currentPair1.Value.GetRawText().Equals(json2.GetRawText()))
                    {
                        errorContainer.JSONValueChanged(currentPair1.Key);
                    }

                }
                // If current property from first file does not exist in second
                else
                {
                    errorContainer.JSONPropertyRemoved(currentPair1.Key);
                }

            }
        }

        public static void CheckPropertyAdded(Dictionary<string, JsonElement> dictionary1, Dictionary<string, JsonElement> dictionary2, ErrorContainer errorContainer, string jsonPath)
        {
            // Check each property and value in json1 to see if each exists and is equal to json2
            foreach (var currentPair2 in dictionary2)
            {
                // If current property from second json file does not exist in the first file
                if (!dictionary1.ContainsKey(currentPair2.Key))
                {
                    errorContainer.JSONPropertyAdded(currentPair2.Key);
                }
            }
        }

        public static void DebugMismatch(ZipArchiveEntry entry, byte[] originalContents, byte[] newContents, string normFormDir)
        {
            // Fail! Mismatch
            Console.WriteLine("FAIL: hash mismatch: " + entry.FullName);

            // Paths to current diff files
            string aPath = normFormDir + "\\" + Path.ChangeExtension(entry.Name, null) + "-A.json";
            string bPath = normFormDir + "\\" + Path.ChangeExtension(entry.Name, null) + "-B.json";

            File.WriteAllBytes(aPath, originalContents);
            File.WriteAllBytes(bPath, newContents);
        }
    }
}
