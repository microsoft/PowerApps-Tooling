// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

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



        [DataTestMethod]
        [DataRow("C5_QbctS/y+6a/vwTGdwZgmDn39Q7FtRfVrO93tXoQn/qc=", @"' ab\r\ncd'")] // whitespace
        [DataRow("C5_QbctS/y+6a/vwTGdwZgmDn39Q7FtRfVrO93tXoQn/qc=", @"'ab\ncd'")] // same
        [DataRow("C5_fiqPYqGq3wwtwdolVxxT4En9IFjhYUmYoiEPT/vaDCk=", @"'ab\ncd  '")] // Trailing whitespace is not trimmed. 
        [DataRow("C5_ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=", "{'InvariantScript' : 'abc'}")]
        [DataRow("C5_ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=", "[ 'abc']")] // InvariantScript is skipped
        [DataRow("C5_bDw5bta1w23K4XInH0YgUbEma4Uekt897qisZUeP1xI=", "1.00")] // 1.0 = 1
        [DataRow("C5_bDw5bta1w23K4XInH0YgUbEma4Uekt897qisZUeP1xI=", "1")]
        [DataRow("C5_a4ayc/80/OGda4BO/1o/V0etpOqiLx1JwB5S3beHW0s=", "\"1\"")] // str and number hash differently
        [DataRow("C5_bDw5bta1w23K4XInH0YgUbEma4Uekt897qisZUeP1xI=", "[ 1 ]")] // array 
        [DataRow("C5_U5T6p9qOA98Qs/n3bCKNf9lQNHwIAJKYfZHR2rnO6eg=", "{'a':1,\r\n'b':2}")]
        [DataRow("C5_U5T6p9qOA98Qs/n3bCKNf9lQNHwIAJKYfZHR2rnO6eg=", "{'b':2,'a':1 }")] // property ordering doesn't matter.
        [DataRow("C5_EqC8ygsfMLdppTgi6G0gq5uj01kbJFxsSPckbfr462E=", "{'b2':2,'a':1 }")] // property name matters
        [DataRow("C5_bjQLnP+zepicpUTmu3gKLHiQHT+zNzh2hRGjBhevoB0=", "null")]
        [DataRow("C5_bjQLnP+zepicpUTmu3gKLHiQHT+zNzh2hRGjBhevoB0=", "false")]
        [DataRow("C5_ypeBEsobvcr6wjGzmiPcTaeG7/gUfE5yuYB3ha/uSLs=", "{ 'a' : { } }")] // Empty object
        [DataRow("C5_ypeBEsobvcr6wjGzmiPcTaeG7/gUfE5yuYB3ha/uSLs=", "[ 'a' ]")] // Empty object
        [DataRow("C5_47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=", "{ 'a' : null }")] // Empty object
        [DataRow("C5_+44g/C5MPySMYMOb1lLzwTRymLuXe4tNWQO4UFViBgM=", "{'a':'b'}")]
        [DataRow("C5_+44g/C5MPySMYMOb1lLzwTRymLuXe4tNWQO4UFViBgM=", "['a', 'b']")] // obj vs. array matters
        public void Checksum(string expectedChecksum, string json)
        {
            var actualChecksum = Check(json);
            Assert.AreEqual(expectedChecksum, actualChecksum);
        }

        // Helper to checksum a json file
        static string Check(string json)
        {
            json = json.Replace('\'', '\"'); // easier esaping
            var bytes = Encoding.UTF8.GetBytes(json);

            byte[] checksum = ChecksumMaker.ChecksumFile<Sha256HashMaker>("test.json", bytes);

            var str = ChecksumMaker.ChecksumToString(checksum);

            return str;
        }
    }
}
