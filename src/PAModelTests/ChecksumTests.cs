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
        [DataRow("MyWeather.msapp", "C1_/zrpJtNl1yARfrGQmlGzz40f1D0OwYERVGSSzF3Inqs=")]
        public void TestChecksum(string filename, string expectedChecksum)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);

            // Checksums should be very stable. 
            var actualChecksum = ChecksumMaker.GetChecksum(root);

            Assert.AreEqual(expectedChecksum, actualChecksum);
        }
    }
}
