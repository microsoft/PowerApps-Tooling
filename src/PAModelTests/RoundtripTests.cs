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
    public class RoundtripTests
    {
        // Apps live in the "Apps" folder, and should have a build action of "Copy to output"
        [DataTestMethod]
        [DataRow("MyWeather.msapp")]
        [DataRow("8bittetris.msapp")]
        public void TestMethod1(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);

            Assert.IsTrue(File.Exists(root));


            bool ok = MsAppTest.StressTest(root);
            Assert.IsTrue(ok);
        }
    }
}
