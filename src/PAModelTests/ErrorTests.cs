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
        public void TestJSONValueChanged()
        {

        string path1 = Path.Combine(Environment.CurrentDirectory, "JSON", "changed1.json");
        string path2 = Path.Combine(Environment.CurrentDirectory, "JSON", "changed2.json");

        ErrorContainer errorContainer = new ErrorContainer();

            byte[] jsonString1 = File.ReadAllBytes(path1);
            byte[] jsonString2 = File.ReadAllBytes(path2);

            var jsonDictionary1 = MsAppTest.FlattenJson(jsonString1);
            var jsonDictionary2 = MsAppTest.FlattenJson(jsonString2);

            // IsMismatched on mismatched files
            MsAppTest.CheckPropertyChangedRemoved(jsonDictionary1, jsonDictionary2, errorContainer, "");
            MsAppTest.CheckPropertyAdded(jsonDictionary1, jsonDictionary2, errorContainer, "");

            // Assume no mismatch
            bool containsJSONMismatch = false;

            // For every error received, see if any is a JSONValueChanged (3013)
            foreach (var error in errorContainer)
            {
                if (error.Code == ErrorCode.JSONValueChanged)
                {
                    containsJSONMismatch = true;
                }
            }

            // Confirm that some error was a JSON Mismatch
            Assert.IsTrue(errorContainer.HasErrors);
            Assert.IsTrue(containsJSONMismatch);
        }

        [TestMethod]
        public void TestJSONPropertyAdded()
        {

            string path1 = Path.Combine(Environment.CurrentDirectory, "JSON", "added1.json");
            string path2 = Path.Combine(Environment.CurrentDirectory, "JSON", "added2.json");

            ErrorContainer errorContainer = new ErrorContainer();

            byte[] jsonString1 = File.ReadAllBytes(path1);
            byte[] jsonString2 = File.ReadAllBytes(path2);

            var jsonDictionary1 = MsAppTest.FlattenJson(jsonString1);
            var jsonDictionary2 = MsAppTest.FlattenJson(jsonString2);

            // IsMismatched on mismatched files
            MsAppTest.CheckPropertyChangedRemoved(jsonDictionary1, jsonDictionary2, errorContainer, "");
            MsAppTest.CheckPropertyAdded(jsonDictionary1, jsonDictionary2, errorContainer, "");

            // Assume no mismatch
            bool containsJSONPropertyAdded = false;

            // For every error received, see if any is a JSONPropertyAdded (3014)
            foreach (var error in errorContainer)
            {
                if (error.Code == ErrorCode.JSONPropertyAdded)
                {
                    containsJSONPropertyAdded = true;
                }
            }

            // Confirm that some error was a JSON Mismatch
            Assert.IsTrue(errorContainer.HasErrors);
            Assert.IsTrue(containsJSONPropertyAdded);
        }

        [TestMethod]
        public void TestJSONPropertyRemoved()
        {

            string path1 = Path.Combine(Environment.CurrentDirectory, "JSON", "removed1.json");
            string path2 = Path.Combine(Environment.CurrentDirectory, "JSON", "removed2.json");

            ErrorContainer errorContainer = new ErrorContainer();

            byte[] jsonString1 = File.ReadAllBytes(path1);
            byte[] jsonString2 = File.ReadAllBytes(path2);

            var jsonDictionary1 = MsAppTest.FlattenJson(jsonString1);
            var jsonDictionary2 = MsAppTest.FlattenJson(jsonString2);

            // IsMismatched on mismatched files
            MsAppTest.CheckPropertyChangedRemoved(jsonDictionary1, jsonDictionary2, errorContainer, "");
            MsAppTest.CheckPropertyAdded(jsonDictionary1, jsonDictionary2, errorContainer, "");

            // Assume no mismatch
            bool containsJSONPropertyRemoved = false;

            // For every error received, see if any is a JSONPropertyRemoved (3015)
            foreach (var error in errorContainer)
            {
                if (error.Code == ErrorCode.JSONPropertyRemoved)
                {
                    containsJSONPropertyRemoved = true;
                }
            }

            // Confirm that some error was a JSON Mismatch
            Assert.IsTrue(errorContainer.HasErrors);
            Assert.IsTrue(containsJSONPropertyRemoved);
        }
    }
}
