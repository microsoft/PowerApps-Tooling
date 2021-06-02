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
        [DataRow("Long*Name*File*Path*Validation*Tests***.fx.yaml", FilePath.MaxFileNameLength, "Long%2aName%2aFile%2aPath%2aValidation%2aTests%2a109.fx.yaml")]
        [DataRow("TestFileName.fx.yaml", 20, "TestFileName.fx.yaml")]
        [DataRow("одиндвао", 8, "одиндвао")] // test with unicode characters.
        public void TestFileNames(string path, int expectedLength, string expectedName)
        {
            var file = new FilePath(path);
            var str = file.ToPlatformPath();
            Assert.AreEqual(str.Length, expectedLength);
            Assert.AreEqual(str, expectedName);
        }
    }
}
