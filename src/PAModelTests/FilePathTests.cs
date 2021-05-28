// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace PAModelTests
{
    [TestClass]
    public class FilePathTests
    {
        // If the length is more that 260 then the file name would be truncated and the new length should be 260
        // Using escaped character to reach max path length with fewer characters.
        [DataTestMethod]
        [DataRow("**********************************************************************************************", FilePath.MAX_PATH)]
        [DataRow("TestFileName.fx.yaml", 20)]
        public void TestValidPath(string path, int expectedLength)
        {
            var str = FilePath.ToValidPath(Utilities.EscapeFilename(path));
            Assert.AreEqual(str.Length, expectedLength);
        }
    }
}
