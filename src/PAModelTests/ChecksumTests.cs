using Microsoft.VisualStudio.TestTools.UnitTesting;
using PAModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PAModelTests
{
    [TestClass]
    public class ChecksumTests
    {
        [DataTestMethod]
        [DataRow("MyWeather.msapp", "C1_0NR4SJOpBD7nc5k/6JoBp0822aEFJyOBo9a7AOvr/Qw=")]
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
