// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace PAModelTests
{
    [TestClass]
    public class ChecksumTests
    {
        [DataTestMethod]
        [DataRow("MyWeather.msapp", "C1_TJ+ZAELmkaG96tYuftn6+qKDyQfo0xga/KKmAXcZ/ek=")]
        public void TestChecksum(string filename, string expectedChecksum)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);

            // Checksums should be very stable. 
            var actualChecksum = ChecksumMaker.GetChecksum(root);

            Assert.AreEqual(expectedChecksum, actualChecksum);
        }

        [DataTestMethod]
        [DataRow("a  bc", "a bc")] 
        [DataRow("  a  b   ", "a b")] // leading, trailing 
        [DataRow("a\t\r\nb", "a b")] // other chars
        public void TestNormWhitespace(string test, string expected)
        {
            var val = ChecksumMaker.NormFormulaWhitespace(test);
            Assert.AreEqual(expected, val);
        }
    }
}
