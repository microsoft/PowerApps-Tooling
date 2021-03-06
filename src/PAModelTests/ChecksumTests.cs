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
        [DataRow("MyWeather.msapp", "C5_dF3ZiT6eOJeXvcVwSRn9PNVFRjYd0wz8Ojtqu0F9iWI=", 11, "References\\DataSources.json", "C5_Q1gxjH2oJoQXvdHbAkE6j5Qgj0Lha8x92iG2m4PwQMY=")]
        public void TestChecksum(string filename, string expectedChecksum, int expectedFileCount, string file, string innerExpectedChecksum)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);

            // Checksums should be very stable. 
            var actualChecksum = ChecksumMaker.GetChecksum(root);

            Assert.AreEqual(expectedChecksum, actualChecksum.wholeChecksum);
            Assert.AreEqual(expectedFileCount, actualChecksum.perFileChecksum.Count);
            Assert.IsTrue(actualChecksum.perFileChecksum.TryGetValue(file, out var perFileChecksum));
            Assert.AreEqual(innerExpectedChecksum, perFileChecksum);
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
