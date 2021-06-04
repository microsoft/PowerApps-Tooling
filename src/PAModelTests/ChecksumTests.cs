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
        [DataRow("MyWeather.msapp", "C6_ZXZwZAG3P0lmCkNAGjsIjYb503akWCyudsk8DEi2aX0=", 11, "References\\DataSources.json", "C6_2dpVudcymwNaHoHtQugF1MSpzsY1I6syuPiB0B+jTYc=")]
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

        // Ensure each fo the checksum constants is unique.
        // this makes it easier to tell when 2 normalized values should be the same. 
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
        const string C1 = "C6_VrBVZxNCyyq6nNHBFhEi1p8/+LEgauFoOxISpTMfidA=";
        const string C2 = "C6_bSgalCJTSBUuXr/l9syvLFQtQveX9OeUkRFO+bXaxsY=";
        const string C3 = "C6_pV4gc7whiqiJA6qNnhtYNgnoy0AV19//Bs/JF+bwvks=";
        const string C4 = "C6_XRxPTS3gnYmfjZL2jlOD/3STXSC8VniAOx8bfu0p3Bc=";
        const string C5 = "C6_6mPsJ1PnWkhrz7kOmeIdrtdFtkUH+WrnfOrR/YWwB4Q=";
        const string C6 = "C6_JpWWJaI11Wjp10J8M8WQ/ZKTOLDqyuh2XpuhrnjNIZI=";
        const string C7 = "C6_SsPqlOsAkyDo7QmBft/8uwvbbDAEpX0rfiYqNUZdC/s=";
        const string C8 = "C6_8ZdpKBDUV+KX/OnFZTsCWB/5mlCFI3DynX5f5H2dN+Y=";
        const string C9 = "C6_mBjUrHTZUVjb7YgLtnZAWXjp25eh8h/uN1rgzH0qTmY=";
        const string C10 = "C6_B0Pu6r/4VrrelJ4VYXbhTNJQfG6zem/OvUxIEweFAa4=";
        const string C11 = "C6_rNmFqjFuTa2nSPd+9J11Dkq2CE1d3UHrdRPbakKNsz8=";

        const string C12 = "C6_xinyFpAi3E1fug553zcwOQphnA26HzffwtFHFYvwBIQ=";
        const string C13 = "C6_tLvF8n0SCv0h+OVpNzeBfjvnMsfWN2SvOttGXgkzud8=";
        const string C14 = "C6_But9amnuGeX733SQGNPSq/oEvL0TZdsxLrhtxxaTibg=";
        const string C15 = "C6_mF50+afGew2Ea527fnP0EhsQSunAh8GbgaBbxpNPKpM=";
        const string C16 = "C6_tpIqQWd4zNcNsjI8xQKjvoocOT+cACVDU9oEmpw/I7Y=";
        const string C17 = "C6_nxln5Ugv128wodPRdYyn8U9mXYzeCvPefCJwkLyof8A=";

        const string C18 = "C6_HyC9hHMBR+0EPsD6yYguZ+cYv2ovwlgEfh3iygNbWHM=";
        const string C19 = "C6_f8iCQ1KTyoVxp/xpbpwNXvjEZgIeNIaTXdzTe0q26ps=";
        const string C20 = "C6_2YUFpLVLEYtdFvV9iLN8F6TM+cWczemMx4m0VEIpfrg=";


        [DataTestMethod]
        [DataRow(C1, @"' ab\r\ncd'")] // whitespace
        [DataRow(C1, @"'ab\ncd'")] // same
        [DataRow(C2, @"'ab\ncd  '")] // Trailing whitespace is not trimmed. 
        [DataRow(C3, "{'InvariantScript' : 'abc'}")]
        [DataRow(C12, "[ 'abc']")] // InvariantScript is skipped
        [DataRow(C4, "1.00")] // 1.0 = 1
        [DataRow(C4, "1")]
        [DataRow(C5, "\"1\"")] // str and number hash differently
        [DataRow(C13, "[ 1 ]")] // BAD: array should be different
        [DataRow(C6, "{'a':1,\r\n'b':2}")]
        [DataRow(C6, "{'b':2,'a':1 }")] // property ordering doesn't matter.
        [DataRow(C7, "{'b2':2,'a':1 }")] // property name matters
        [DataRow(C8, "null")]
        [DataRow(C14, "false")] // null!=false
        [DataRow(C9, "{ 'a' : { } }")] // Empty object
        [DataRow(C15, "[ 'a' ]")] // Empty object
        [DataRow(C10, "{ 'a' : null }")] // Empty object
        [DataRow(C11, "{'a':'b'}")]
        [DataRow(C16, "['a', 'b']")] // obj vs. array matters
        [DataRow(C17, "['ab']")] // concatenation should be different.
        [DataRow(C18, "{'LocalDatabaseReferences' : '' }")] // Double encoded json, per LocalDatabaseReferences name.
        [DataRow(C19, "{'LocalDatabaseReferences' : '{}' }")] // Should be different
        [DataRow(C20, "{'LocalDatabaseReferences' : '[]' }")] // Should be different
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
