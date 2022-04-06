// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.Json;
using Xunit;


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
        [Fact]
        public void OpenCorruptedMsApp()
        {
            // Empty stream is invalid document, should generate a Read error.
            MemoryStream ms = new MemoryStream();

            (var doc, var errors) = CanvasDocument.LoadFromMsapp(ms);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(errors.HasErrors);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(doc);
        }

        [TestMethod]
        [Fact]
        public void OpenMissingMsApp()
        {
            (var doc, var errors) = CanvasDocument.LoadFromMsapp(PathMissingMsapp);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(errors.HasErrors);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(doc);
        }

        [TestMethod]
        [Fact]
        public void OpenBadSources()
        {
            // Common error can be mixing up arguments. Ensure that we detect passing a valid msapp as a source param.
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(File.Exists(PathToValidMsapp));

            (var doc, var errors) = CanvasDocument.LoadFromSources(PathToValidMsapp);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(errors.HasErrors);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(doc);            
        }

        [TestMethod]
        [Fact]
        public void OpenMissingSources()
        {
            (var doc, var errors) = CanvasDocument.LoadFromSources(PathMissingDir);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(errors.HasErrors);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(doc);
        }

        [Theory]
        [InlineData("changed1.json", "changed2.json", true, false, false)]
        [InlineData("added1.json", "added2.json", false, true, false)]
        [InlineData("removed1.json", "removed2.json", false, false, true)]
        public void TestJSONValueChanged(string file1, string file2, bool changed, bool added, bool removed)
        {

        string path1 = Path.Combine(Environment.CurrentDirectory, "JSON", file1);
        string path2 = Path.Combine(Environment.CurrentDirectory, "JSON", file2);

        ErrorContainer errorContainer = new ErrorContainer();

            byte[] jsonString1 = File.ReadAllBytes(path1);
            byte[] jsonString2 = File.ReadAllBytes(path2);

            var jsonDictionary1 = MsAppTest.FlattenJson(jsonString1);
            var jsonDictionary2 = MsAppTest.FlattenJson(jsonString2);

            // IsMismatched on mismatched files
            MsAppTest.CheckPropertyChangedRemoved(jsonDictionary1, jsonDictionary2, errorContainer, "");
            MsAppTest.CheckPropertyAdded(jsonDictionary1, jsonDictionary2, errorContainer, "");

            // Assume no mismatch
            bool JSONPropertyAdded = false;
            bool JSONPropertyRemoved = false;
            bool JSONValueChanged = false;

            // For every error received, check JSON Mismatches
            foreach (var error in errorContainer)
            {
                if (error.Code == ErrorCode.JSONPropertyAdded)
                {
                    JSONPropertyAdded = true;
                }
                else if (error.Code == ErrorCode.JSONPropertyRemoved)
                {
                    JSONPropertyRemoved = true;
                }
                else if (error.Code == ErrorCode.JSONValueChanged)
                {
                    JSONValueChanged = true;
                }
            }

            // Confirm that some error was a JSON Mismatch
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(errorContainer.HasErrors);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(JSONValueChanged == changed);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(JSONPropertyAdded == added);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(JSONPropertyRemoved == removed);
        }
    }
}
