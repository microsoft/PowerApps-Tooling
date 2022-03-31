// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

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
              
            // CheckPropertyMismatch on mismatched files
            CheckPropertyMismatchOne(PathMismatchJSON1, PathMismatchJSON2, errorContainer);
            CheckPropertyMismatchTwo(PathMismatchJSON1, PathMismatchJSON2, errorContainer);

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
