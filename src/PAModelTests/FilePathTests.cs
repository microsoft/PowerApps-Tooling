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
        // If the length of the escaped file name is beyond 60, then we truncate the length to limit it to 60.
        [DataTestMethod]
        [DataRow("Long*Name*File*Path*Validation*Tests***.fx.yaml", FilePath.MaxFileNameLength)]
        [DataRow("TestFileName.fx.yaml", 20)]
        [DataRow("одиндвао", 8)] // test with unicode characters.
        public void TestFileNames(string path, int expectedLength)
        {
            var file = new FilePath(path);
            var str = file.ToPlatformPath();
            Assert.AreEqual(str.Length, expectedLength);
        }
    }
}
