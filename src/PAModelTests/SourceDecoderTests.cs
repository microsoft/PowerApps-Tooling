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
        [DataRow("MyWeather.msapp", "Screen1.pa.yaml", "Weather_Screen1.pa.yaml")]
        [DataRow("GroupControlTest.msapp", "Screen1.pa.yaml", "GroupControl_Test.pa.yaml")]
        [DataRow("GalleryTestApp.msapp", "Screen1.pa.yaml", "Gallery_ScreenTest.pa.yaml")]
        [DataRow("SimpleScopeVariables.msapp", "Components\\Component1.pa.yaml", "ComponentFunction_Test.pa.yaml")]
        [DataRow("TestStudio_Test.msapp", "Tests\\Test_7F478737223C4B69.pa.yaml", "TestStudio_Test.pa.yaml")]
        [DataRow("autolayouttest.msapp", "Screen1.pa.yaml", "AutoLayout_Test.pa.yaml")]
        public void TestScreenBaselines(string appName, string sourceFileName, string screenBaselineName)
        {
            // Pull both the msapp and the baseline from our embedded resources. 
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            // validate the source 
            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;
                msapp.SaveToSources(outSrcDir);

                var pathActual = Path.Combine(outSrcDir, "Src", sourceFileName);
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
