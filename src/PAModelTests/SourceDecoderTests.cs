// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace PAModelTests
{
    // Test that a series of .msapps can succeesfully roundtrip. 
    [TestClass]
    public class SourceDecoderTests
    {
        // Compare actual source output. This catches things like:
        // - are we removing default properties, from both Theme Json and Template xmL?
        // - canonical ordering and stable output 
        [DataTestMethod]
        [DataRow("MyWeather.msapp", "Weather_Screen1.pa1")]
        [DataRow("GalleryTestApp.msapp", "Gallery_ScreenTest.pa1")]
        public void TestScreenBaselines(string appName, string screenBaselineName)
        {
            // Pull both the msapp and the baseline from our embedded resources. 
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            var msapp = CanvasDocument.LoadFromMsapp(root);

            // validate the source 
            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;
                msapp.SaveAsSource(outSrcDir);

                var pathActual = Path.Combine(outSrcDir, "Src", "Screen1.pa.yaml");
                var pathExpected = Path.Combine(Environment.CurrentDirectory, "SrcBaseline", screenBaselineName);


                // TODO - as the format stabalizes, we can compare more aggressively.
                AssertFilesEqual(pathExpected, pathActual);
            }
        }

        static void AssertFilesEqual(string pathExpected, string pathActual)
        {
            var expected = File.ReadAllText(pathExpected).Replace("\r\n", "\n").Trim();
            var actual = File.ReadAllText(pathActual).Replace("\r\n", "\n").Trim();

            Assert.AreEqual(expected, actual);
        }
    }
}
