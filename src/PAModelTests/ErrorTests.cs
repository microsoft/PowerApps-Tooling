// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.Json;


namespace PAModelTests
{
    // Verify we get errors (and not exceptions). 
    [TestClass]
    public class ErrorTests
    {
        public static string PathToValidMsapp = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");

        public static string PathMissingMsapp = Path.Combine(Environment.CurrentDirectory, "Missing", "Missing.msapp");

        public static string PathMissingDir = Path.Combine(Environment.CurrentDirectory, "MissingDirectory");

        public static string PathMismatchJSON1 = Path.Combine(Environment.CurrentDirectory, "JSON", "mismatched1.json");

        public static string PathMismatchJSON2 = Path.Combine(Environment.CurrentDirectory, "JSON", "mismatched2.json");


        [TestMethod]
        public void OpenCorruptedMsApp()
        {
            // Empty stream is invalid document, should generate a Read error.
            MemoryStream ms = new MemoryStream();

            (var doc, var errors) = CanvasDocument.LoadFromMsapp(ms);
            Assert.IsTrue(errors.HasErrors);
            Assert.IsNull(doc);
        }

        [TestMethod]
        public void OpenMissingMsApp()
        {
            (var doc, var errors) = CanvasDocument.LoadFromMsapp(PathMissingMsapp);
            Assert.IsTrue(errors.HasErrors);
            Assert.IsNull(doc);
        }

        [TestMethod]
        public void OpenBadSources()
        {
            // Common error can be mixing up arguments. Ensure that we detect passing a valid msapp as a source param.
            Assert.IsTrue(File.Exists(PathToValidMsapp));

            (var doc, var errors) = CanvasDocument.LoadFromSources(PathToValidMsapp);
            Assert.IsTrue(errors.HasErrors);
            Assert.IsNull(doc);            
        }

        [TestMethod]
        public void OpenMissingSources()
        {
            (var doc, var errors) = CanvasDocument.LoadFromSources(PathMissingDir);
            Assert.IsTrue(errors.HasErrors);
            Assert.IsNull(doc);
        }

        [TestMethod]
        public void TestJSONMismatchErrorGiven()
        {
            ErrorContainer errorContainer = new ErrorContainer();

            string jsonPath1 = File.ReadAllText(PathMismatchJSON1);
            string jsonPath2 = File.ReadAllText(PathMismatchJSON2);

            JsonElement json1 = JsonSerializer.Deserialize<JsonElement>(jsonPath1) !;
            JsonElement json2 = JsonSerializer.Deserialize<JsonElement>(jsonPath2) !;

            // CheckPropertyMismatch on mismatched files
            MsAppTest.CheckPropertyMismatchOne(json1, json2, errorContainer);
            MsAppTest.CheckPropertyMismatchTwo(json1, json2, errorContainer);

            // Assume no mismatch
            bool containsJSONMismatch = false;

            // For every error received, see if any is a JSONMismatch (3013)
            foreach (var error in errorContainer)
            {
                if (error.Code == (ErrorCode)3013)
                {
                    containsJSONMismatch = true;
                }
            }

            // Confirm that some error was a JSONMismatch
            Assert.IsTrue(containsJSONMismatch);
        }
    }
}
