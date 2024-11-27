// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Text;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PAModelTests;

[TestClass]
public class ChecksumTests
{
    [DataTestMethod]
    [DataRow("MyWeather.msapp", "C8_ZXZwZAG3P0lmCkNAGjsIjYb503akWCyudsk8DEi2aX0=", 11, "References\\DataSources.json", "C8_2dpVudcymwNaHoHtQugF1MSpzsY1I6syuPiB0B+jTYc=")]
    public void TestChecksum(string filename, string expectedChecksum, int expectedFileCount, string file, string innerExpectedChecksum)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);

        // Checksums should be very stable. 
        var actualChecksum = ChecksumMaker.GetChecksum(root);

        Assert.AreEqual(expectedChecksum, actualChecksum.wholeChecksum);
        Assert.AreEqual(expectedFileCount, actualChecksum.perFileChecksum.Count);
        Assert.IsTrue(actualChecksum.perFileChecksum.TryGetValue(file, out var perFileChecksum));
        Assert.AreEqual(innerExpectedChecksum, perFileChecksum);

        // Test checksum version
        Assert.AreEqual(ChecksumMaker.GetChecksumVersion(expectedChecksum), ChecksumMaker.Version);
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
        var checksums = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prop in GetType().GetFields(BindingFlags.Static | BindingFlags.NonPublic))
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
    private const string C1 = "C8_VrBVZxNCyyq6nNHBFhEi1p8/+LEgauFoOxISpTMfidA=";
    private const string C2 = "C8_bSgalCJTSBUuXr/l9syvLFQtQveX9OeUkRFO+bXaxsY=";
    private const string C3 = "C8_pV4gc7whiqiJA6qNnhtYNgnoy0AV19//Bs/JF+bwvks=";
    private const string C4 = "C8_XRxPTS3gnYmfjZL2jlOD/3STXSC8VniAOx8bfu0p3Bc=";
    private const string C5 = "C8_6mPsJ1PnWkhrz7kOmeIdrtdFtkUH+WrnfOrR/YWwB4Q=";
    private const string C6 = "C8_JpWWJaI11Wjp10J8M8WQ/ZKTOLDqyuh2XpuhrnjNIZI=";
    private const string C7 = "C8_SsPqlOsAkyDo7QmBft/8uwvbbDAEpX0rfiYqNUZdC/s=";
    private const string C8 = "C8_8ZdpKBDUV+KX/OnFZTsCWB/5mlCFI3DynX5f5H2dN+Y=";
    private const string C9 = "C8_mBjUrHTZUVjb7YgLtnZAWXjp25eh8h/uN1rgzH0qTmY=";
    private const string C10 = "C8_B0Pu6r/4VrrelJ4VYXbhTNJQfG6zem/OvUxIEweFAa4=";
    private const string C11 = "C8_rNmFqjFuTa2nSPd+9J11Dkq2CE1d3UHrdRPbakKNsz8=";
    private const string C12 = "C8_xinyFpAi3E1fug553zcwOQphnA26HzffwtFHFYvwBIQ=";
    private const string C13 = "C8_tLvF8n0SCv0h+OVpNzeBfjvnMsfWN2SvOttGXgkzud8=";
    private const string C14 = "C8_But9amnuGeX733SQGNPSq/oEvL0TZdsxLrhtxxaTibg=";
    private const string C15 = "C8_mF50+afGew2Ea527fnP0EhsQSunAh8GbgaBbxpNPKpM=";
    private const string C16 = "C8_tpIqQWd4zNcNsjI8xQKjvoocOT+cACVDU9oEmpw/I7Y=";
    private const string C17 = "C8_nxln5Ugv128wodPRdYyn8U9mXYzeCvPefCJwkLyof8A=";
    private const string C18 = "C8_HyC9hHMBR+0EPsD6yYguZ+cYv2ovwlgEfh3iygNbWHM=";
    private const string C19 = "C8_f8iCQ1KTyoVxp/xpbpwNXvjEZgIeNIaTXdzTe0q26ps=";
    private const string C20 = "C8_2YUFpLVLEYtdFvV9iLN8F6TM+cWczemMx4m0VEIpfrg=";


    [DataTestMethod]
    [DataRow(C1, /*lang=json*/ @"' ab\r\ncd'")] // whitespace
    [DataRow(C1, /*lang=json*/ @"'ab\ncd'")] // same
    [DataRow(C2, /*lang=json*/ @"'ab\ncd  '")] // Trailing whitespace is not trimmed. 
    [DataRow(C3, /*lang=json*/ "{'InvariantScript' : 'abc'}")]
    [DataRow(C12, /*lang=json*/ "[ 'abc']")] // InvariantScript is skipped
    [DataRow(C4, /*lang=json*/ "1.00")] // 1.0 = 1
    [DataRow(C4, /*lang=json*/ "1")]
    [DataRow(C5, /*lang=json*/ "\"1\"")] // str and number hash differently
    [DataRow(C13, /*lang=json*/ "[ 1 ]")] // BAD: array should be different
    [DataRow(C6, /*lang=json*/ "{'a':1,\r\n'b':2}")]
    [DataRow(C6, /*lang=json*/ "{'b':2,'a':1 }")] // property ordering doesn't matter.
    [DataRow(C7, /*lang=json*/ "{'b2':2,'a':1 }")] // property name matters
    [DataRow(C8, /*lang=json*/ "null")]
    [DataRow(C14, /*lang=json*/ "false")] // null!=false
    [DataRow(C9, /*lang=json*/ "{ 'a' : { } }")] // Empty object
    [DataRow(C15, /*lang=json*/ "[ 'a' ]")] // Empty object
    [DataRow(C10, /*lang=json*/ "{ 'a' : null }")] // Empty object
    [DataRow(C11, /*lang=json*/ "{'a':'b'}")]
    [DataRow(C16, /*lang=json*/ "['a', 'b']")] // obj vs. array matters
    [DataRow(C17, /*lang=json*/ "['ab']")] // concatenation should be different.
    [DataRow(C18, /*lang=json*/ "{'LocalDatabaseReferences' : '' }")] // Double encoded json, per LocalDatabaseReferences name.
    [DataRow(C19, /*lang=json*/ "{'LocalDatabaseReferences' : '{}' }")] // Should be different
    [DataRow(C20, /*lang=json*/ "{'LocalDatabaseReferences' : '[]' }")] // Should be different
    public void Checksum(string expectedChecksum, string json)
    {
        var actualChecksum = Check(json);

        // If this fails, you can run the checksum with <DebugTextHashMaker> instead. 
        Assert.AreEqual(expectedChecksum, actualChecksum);
    }

    // Helper to checksum a json file
    private static string Check(string json)
    {
        json = json.Replace('\'', '\"'); // easier esaping
        var bytes = Encoding.UTF8.GetBytes(json);

        var checksum = ChecksumMaker.ChecksumFile<Sha256HashMaker>("test.json", bytes);

        var str = ChecksumMaker.ChecksumToString(checksum);

        return str;
    }
}
