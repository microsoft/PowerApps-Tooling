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
        // - are we removing default properties
        // - canonical ordering and stable output 
        [TestMethod]
        public void TestMethod1()
        {
            // Pull both the msapp and the baseline from our embedded resources. 
            var filename = "MyWeather.msapp";
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            var msapp = CanvasDocument.LoadFromMsapp(root);

            // validate the source 
            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;
                msapp.SaveAsSource(outSrcDir);

                var pathActual = Path.Combine(outSrcDir, "Src", "Screen1.pa1");
                var pathExpected = Path.Combine(Environment.CurrentDirectory, "SrcBaseline", "Weather_Screen1.pa1");


                // TODO - as the format stabalizes, we can compare more aggressively.
                AssertFilesEqual(pathExpected, pathActual);
            }
        }

        static void AssertFilesEqual(string pathExpected, string pathActual)
        {
            var expected = File.ReadAllText(pathExpected).Trim();
            var actual = File.ReadAllText(pathActual).Trim();            

            Assert.AreEqual(expected, actual);
        }
    }
}
