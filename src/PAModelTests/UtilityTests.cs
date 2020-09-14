// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PAModelTests
{
    [TestClass]
    public class UtilityTests
    {
        [DataTestMethod]
        [DataRow("\r\t!$/\\^%", "%0d%09%21%24%2f%5c%5e%25")]
        [DataRow("\u4523", "%%4523")]
        public void TestEscaping(string unescaped, string escaped)
        {
            Assert.AreEqual(escaped, Utility.EscapeFilename(unescaped));
            Assert.AreEqual(unescaped, Utility.UnEscapeFilename(escaped));
        }

        [TestMethod]
        public void TestNotEscaped()
        {
            // Not escaped.
            var a = "0123456789AZaz[]_.";
            Assert.AreEqual(a, Utility.EscapeFilename(a));
        }
    }
}
