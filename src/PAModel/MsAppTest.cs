// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools.Extensions;
using Microsoft.PowerPlatform.Formulas.Tools.IO;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool;
using Microsoft.PowerPlatform.Formulas.Tools.MergeTool.Deltas;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace Microsoft.PowerPlatform.Formulas.Tools;

internal class MsAppTest
{
    public static bool Compare(CanvasDocument doc1, CanvasDocument doc2)
    {
        using var temp1 = new TempFile();
        using var temp2 = new TempFile();
        doc1.SaveToMsApp(temp1.FullPath);
        doc2.SaveToMsApp(temp2.FullPath);
        return Compare(temp1.FullPath, temp2.FullPath);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
    public static bool MergeStressTest(string pathToMsApp1, string pathToMsApp2)
    {
        (var doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp1);
        errors.ThrowOnErrors();

        (var doc2, var errors2) = CanvasDocument.LoadFromMsapp(pathToMsApp2);
        errors2.ThrowOnErrors();

        var doc1New = CanvasMerger.Merge(doc1, doc2, doc2);
        var ok1 = HasNoDeltas(doc1, doc1New);

        var doc2New = CanvasMerger.Merge(doc2, doc1, doc1);
        var ok2 = HasNoDeltas(doc2, doc2New);

        return ok1 && ok2;
    }

    public static bool TestClone(string pathToMsApp)
    {
        (var doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        errors.ThrowOnErrors();

        var docClone = new CanvasDocument(doc1);

        return HasNoDeltas(doc1, docClone, strict: true);
    }

    public static bool DiffStressTest(string pathToMsApp)
    {
        (var doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        errors.ThrowOnErrors();

        return HasNoDeltas(doc1, doc1);
    }

    // Verify there are no deltas (detected via smart merge) between doc1 and doc2
    // Strict =true, also compare entropy files.
    private static bool HasNoDeltas(CanvasDocument doc1, CanvasDocument doc2, bool strict = false)
    {
        var ourDeltas = Diff.ComputeDelta(doc1, doc1);

        // ThemeDelta always added
        ourDeltas = ourDeltas.Where(x => x.GetType() != typeof(ThemeChange));

        if (ourDeltas.Any())
        {
            // Error! app shouldn't have any diffs with itself.
            return false;
        }


        // Save and verify checksums.
        using var temp1 = new TempFile();
        using var temp2 = new TempFile();
        doc1.SaveToMsApp(temp1.FullPath);
        doc2.SaveToMsApp(temp2.FullPath);

        bool same;
        if (strict)
        {
            same = Compare(temp1.FullPath, temp2.FullPath);
        }
        else
        {
            var doc1NoEntropy = RemoveEntropy(temp1.FullPath);
            var doc2NoEntropy = RemoveEntropy(temp2.FullPath);

            same = Compare(doc1NoEntropy, doc2NoEntropy);
        }

        return same;
    }

    // Unpack, delete the entropy dirs, repack.
    public static CanvasDocument RemoveEntropy(string pathToMsApp)
    {
        using var temp1 = new TempDir();
        (var doc1, var errors) = CanvasDocument.LoadFromMsapp(pathToMsApp);
        errors.ThrowOnErrors();

        doc1.SaveToSources(temp1.Dir);

        var entropyDir = Path.Combine(temp1.Dir, "Entropy");
        if (!Directory.Exists(entropyDir))
        {
            throw new Exception($"Missing entropy dir: " + entropyDir);
        }

        Directory.Delete(entropyDir, recursive: true);
        (var doc2, _) = CanvasDocument.LoadFromSources(temp1.Dir);
        errors.ThrowOnErrors();

        return doc2;
    }

    /// <summary>
    /// Given an msapp (original source of truth), stress test the conversions
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
    public static bool StressTest(string pathToMsApp)
    {
        using (var temp1 = new TempFile())
        {
            var outFile = temp1.FullPath;

            // MsApp --> Model
            CanvasDocument msapp;
            var errors = new ErrorContainer();
            try
            {
                using (var stream = new FileStream(pathToMsApp, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    msapp = MsAppSerializer.Load(stream, errors);
                }
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

            if (!Compare(pathToMsApp, outFile, errors))
            {
                errors.ThrowOnErrors();
                return false;
            }

            // Model --> Source
            using var tempDir = new TempDir();
            var outSrcDir = tempDir.Dir;
            errors = msapp.SaveToSources(outSrcDir, verifyOriginalPath: pathToMsApp);
            errors.ThrowOnErrors();
        } // end using

        if (!TestClone(pathToMsApp))
        {
            return false;
        }

        if (!DiffStressTest(pathToMsApp))
        {
            return false;
        }

        return true;
    }

    public static bool Compare(string pathToZip1, string pathToZip2)
    {
        var errorContainer = new ErrorContainer();
        return Compare(pathToZip1, pathToZip2, errorContainer);
    }

    // Overload with ErrorContainer
    public static bool Compare(string pathToZip1, string pathToZip2, ErrorContainer errorContainer)
    {
        if (ChecksumMaker.GetChecksum(pathToZip1).wholeChecksum == ChecksumMaker.GetChecksum(pathToZip2).wholeChecksum)
        {
            return true;
        }

        // Provide a comparison that can be very specific about what the difference is.
        var comp = new Dictionary<string, byte[]>();

        CompareChecksums(pathToZip1, comp, true, errorContainer);
        CompareChecksums(pathToZip2, comp, false, errorContainer);

        return false;
    }

    // Compare the debug checksums.
    // Get a hash for the MsApp file.
    // First pass adds file/hash to comp.
    // Second pass checks hash equality and removes files from comp.
    // After second pass, comp should be 0. Any files in comp were missing from 2nd pass.
    private static void CompareChecksums(string pathToZip, Dictionary<string, byte[]> comp, bool first, ErrorContainer errorContainer)
    {
        // Path to the directory where we are creating the normalized form
        var normFormDir = ".\\diffFiles";

        // Create directory if doesn't exist
        if (!Directory.Exists(normFormDir))
        {
            Directory.CreateDirectory(normFormDir);
        }

        using var zip = ZipFile.OpenRead(pathToZip);
        foreach (var entry in zip.Entries.OrderBy(x => x.FullName))
        {
            var newContents = ChecksumMaker.ChecksumFile<DebugTextHashMaker>(entry.FullName, entry.ToBytes());
            if (newContents == null)
            {
                continue;
            }

            // Do easy diffs
            var entryFullName = entry.FullName;
            if (first)
            {
                comp.Add(entryFullName, newContents);
            }
            else
            {
                if (comp.TryGetValue(entryFullName, out var originalContents))
                {
                    CompareEntryContents(entryFullName, originalContents, newContents, errorContainer);

                    comp.Remove(entryFullName);
                }
                else
                {
                    // Missing file!
                    errorContainer.AddedZipEntry(entryFullName);
                }
            }
        }
    }

    private static void CompareEntryContents(string entryFullName, byte[] originalContents, byte[] newContents, ErrorContainer errorContainer)
    {
        if (!newContents.SequenceEqual(originalContents))
        {
            // Catch in case of originalContents/newContents not being JSON
            try
            {
                JsonDocument.Parse(originalContents);
                JsonDocument.Parse(newContents);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Mismatch detected in non-Json properties: " + entryFullName, ex);
            }

            var flattenedJsonOrig = FlattenJson(originalContents);
            var flattenedJsonNew = FlattenJson(newContents);

            // Add JSONMismatch error if JSON property was changed or removed
            CheckPropertyChangedRemoved(entryFullName, flattenedJsonOrig, flattenedJsonNew, errorContainer);

            // Add JSONMismatch error if JSON property was added
            CheckPropertyAdded(entryFullName, flattenedJsonOrig, flattenedJsonNew, errorContainer);
        }
    }

    public static Dictionary<string, JsonElement> FlattenJson(byte[] json)
    {
        using var document = JsonDocument.Parse(json);
        return FlattenJson(string.Empty, document.RootElement)
            .ToDictionary(t => t.Path, t => t.Value.Clone());
    }

    private static IEnumerable<(string Path, JsonElement Value)> FlattenJson(string path, JsonElement value)
    {
        Debug.Assert(path is not null);

        if (value.ValueKind == JsonValueKind.Object)
        {
            return FlattenObject(path, value);
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            // Only flatten array if it has an object as one of its items. Otherwise, treat the entire array as a leaf.
            if (value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Object))
            {
                return FlattenArray(path, value);
            }
        }

        return [(path, value)];
    }

    public static IEnumerable<(string Path, JsonElement Value)> FlattenObject(string path, JsonElement jsonObject)
    {
        Debug.Assert(path is not null);
        Debug.Assert(jsonObject.ValueKind == JsonValueKind.Object);

        // pre-append the '.' operator if not the root object
        if (path.Length > 0)
        {
            path += ".";
        }

        return jsonObject.EnumerateObject()
            .SelectMany(property => FlattenJson(path + property.Name, property.Value));
    }

    public static IEnumerable<(string Path, JsonElement Value)> FlattenArray(string path, JsonElement jsonArray)
    {
        Debug.Assert(path is not null);
        Debug.Assert(jsonArray.ValueKind == JsonValueKind.Array);

        var isRulesArray = path.StartsWith("TopParent.") && path.EndsWith(".Rules");

        return jsonArray.EnumerateArray()
            .SelectMany((arrayItem, index) =>
            {
                var arraySubPath = $"{path}[{index}]";

                // For Rules arrays, use the Property name as the key instead of the index, since order doesn't matter and Property is (likely) unique
                if (isRulesArray && arrayItem.ValueKind == JsonValueKind.Object && arrayItem.TryGetProperty("Property", out var propertyName))
                {
                    arraySubPath = $"{path}['{propertyName.GetString()}']";
                }

                return FlattenJson(arraySubPath, arrayItem);
            });
    }

    public static void CheckPropertyChangedRemoved(string entryFullName, Dictionary<string, JsonElement> flattenedJsonOrig, Dictionary<string, JsonElement> flattenedJsonNew, ErrorContainer errorContainer)
    {
        foreach (var kvpOrig in flattenedJsonOrig)
        {
            // Check if the property exists in the new JSON
            if (flattenedJsonNew.TryGetValue(kvpOrig.Key, out var newJsonValue))
            {
                // Check if the raw value is different, if so, it's a mismatch
                if (!kvpOrig.Value.GetRawText().Equals(newJsonValue.GetRawText()))
                {
                    errorContainer.JSONValueChanged(entryFullName, kvpOrig.Key);
                }
            }
            else // Then the property was removed
            {
                errorContainer.JSONPropertyRemoved(entryFullName, kvpOrig.Key);
            }
        }
    }

    public static void CheckPropertyAdded(string entryFullName, Dictionary<string, JsonElement> flattenedJsonOrig, Dictionary<string, JsonElement> flattenedJsonNew, ErrorContainer errorContainer)
    {
        // Report any properties in new that are not in original
        foreach (var newPath in flattenedJsonNew.Keys.Except(flattenedJsonOrig.Keys))
        {
            errorContainer.JSONPropertyAdded(entryFullName, newPath);
        }
    }

    public static void DebugMismatch(ZipArchiveEntry entry, byte[] originalContents, byte[] newContents, string normFormDir)
    {
        // Fail! Mismatch
        //Console.WriteLine("FAIL: hash mismatch: " + entry.FullName);

        // Paths to current diff files
        var aPath = normFormDir + "\\" + Path.ChangeExtension(entry.Name, null) + "-A.json";
        var bPath = normFormDir + "\\" + Path.ChangeExtension(entry.Name, null) + "-B.json";

        File.WriteAllBytes(aPath, originalContents);
        File.WriteAllBytes(bPath, newContents);
    }
}
