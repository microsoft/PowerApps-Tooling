// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        // Ensure each fo the checksum constant is unique. 
        [TestMethod]
        public void ChecksumsUnique()
        {
            HashSet<string> checksums = new HashSet<string>(StringComparer.Ordinal);
            foreach(var prop in this.GetType().GetFields(BindingFlags.Static | BindingFlags.NonPublic ))
            {
                if (prop.Name.StartsWith("C"))
                {
                    var val = prop.GetValue(null).ToString();
                    Assert.IsTrue(checksums.Add(val));
                }
            }
        }

        // Physically check in the actual checksum results.
        // Checksum must be very stable since these get checked into all our customers now.
        // Use const fields to help for cases when checksums should be the same. 
        const string C1 = "C5_QbctS/y+6a/vwTGdwZgmDn39Q7FtRfVrO93tXoQn/qc=";
        const string C2 = "C5_fiqPYqGq3wwtwdolVxxT4En9IFjhYUmYoiEPT/vaDCk=";
        const string C3 = "C5_ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=";
        const string C4 = "C5_bDw5bta1w23K4XInH0YgUbEma4Uekt897qisZUeP1xI=";
        const string C5 = "C5_a4ayc/80/OGda4BO/1o/V0etpOqiLx1JwB5S3beHW0s=";
        const string C6 = "C5_U5T6p9qOA98Qs/n3bCKNf9lQNHwIAJKYfZHR2rnO6eg=";
        const string C7 = "C5_EqC8ygsfMLdppTgi6G0gq5uj01kbJFxsSPckbfr462E=";
        const string C8 = "C5_bjQLnP+zepicpUTmu3gKLHiQHT+zNzh2hRGjBhevoB0=";
        const string C9 = "C5_ypeBEsobvcr6wjGzmiPcTaeG7/gUfE5yuYB3ha/uSLs=";
        const string C10 = "C5_47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=";
        const string C11 = "C5_+44g/C5MPySMYMOb1lLzwTRymLuXe4tNWQO4UFViBgM=";

        [DataTestMethod]
        [DataRow(C1, @"' ab\r\ncd'")] // whitespace
        [DataRow(C1, @"'ab\ncd'")] // same
        [DataRow(C2, @"'ab\ncd  '")] // Trailing whitespace is not trimmed. 
        [DataRow(C3, "{'InvariantScript' : 'abc'}")]
        [DataRow(C3, "[ 'abc']")] // BAD: InvariantScript is skipped
        [DataRow(C4, "1.00")] // 1.0 = 1
        [DataRow(C4, "1")]
        [DataRow(C5, "\"1\"")] // str and number hash differently
        [DataRow(C4, "[ 1 ]")] // BAD: array should be different
        [DataRow(C6, "{'a':1,\r\n'b':2}")]
        [DataRow(C6, "{'b':2,'a':1 }")] // property ordering doesn't matter.
        [DataRow(C7, "{'b2':2,'a':1 }")] // property name matters
        [DataRow(C8, "null")]
        [DataRow(C8, "false")] // BAD: null!=false
        [DataRow(C9, "{ 'a' : { } }")] // Empty object
        [DataRow(C9, "[ 'a' ]")] // BAD: Empty object
        [DataRow(C10, "{ 'a' : null }")] // Empty object
        [DataRow(C11, "{'a':'b'}")]
        [DataRow(C11, "['a', 'b']")] // BAD: obj vs. array matters
        [DataRow(C11, "['ab']")] // BAD: concatenation should be different.
        public void Checksum(string expectedChecksum, string json)
        {
            var actualChecksum = Check(json);

            // If this fails, you can run the checksum with <DebugTextHashMaker> instead. 
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
