// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace PAModelTests
{
    [TestClass]
    public class UtilityTests
    {
        [DataTestMethod]
        [DataRow("\r\t!$^%/\\", "%0d%09%21%24%5e%25%2f%5c")]
        [DataRow("одиндваодиндваодиндваодиндваодиндваодинд", "одиндваодиндваодиндваодиндваодиндваодинд")]
        public void TestEscaping(string unescaped, string escaped)
        {
            Assert.AreEqual(Utilities.EscapeFilename(unescaped), escaped);
            Assert.AreEqual(Utilities.UnEscapeFilename(escaped), unescaped);
        }

        [DataTestMethod]
        [DataRow("foo-%41", "foo-A")]
        [DataRow("[]_' ", "[]_' ")] // unescape only touches % character.
        public void TestUnescape(string escaped, string unescaped)
        {
            Assert.AreEqual(Utilities.UnEscapeFilename(escaped), unescaped);
        }

        [TestMethod]
        public void TestNotEscaped()
        {
            // Not escaped.
            var a = "0123456789AZaz[]_. ";
            Assert.AreEqual(Utilities.EscapeFilename(a), a);
        }

        [DataTestMethod]
        [DataRow("C:\\Foo\\Bar\\file", "C:\\Foo", "Bar\\file")]
        [DataRow("C:\\Foo\\Bar\\file", "C:\\Foo\\", "Bar\\file")]
        [DataRow("C:\\Foo\\Bar.msapp", "C:\\Foo", "Bar.msapp")]
        [DataRow("C:\\Foo\\Bar.msapp", "C:\\Foo\\", "Bar.msapp")]
        [DataRow("C:\\Foo\\Bar.msapp", "C:\\", "Foo\\Bar.msapp")]
        [DataRow(@"C:\DataSources\JourneyPlanner|Sendforapproval.json", "C:\\", @"DataSources\JourneyPlanner|Sendforapproval.json")]
        [DataRow(@"C:\DataSources\JourneyPlanner%7cSendforapproval.json", "C:\\", @"DataSources\JourneyPlanner%7cSendforapproval.json")]
        [DataRow(@"d:\app\Src\EditorState\Screen%252.editorstate.json", @"d:\app", @"Src\EditorState\Screen%252.editorstate.json")]
        [DataRow(@"C:\Temp\MySolution\MySolution.Project\DataSources\JourneyPlanner%7cSendforapproval.json", @"C:\Temp\MySolution\MySolution.Project", @"DataSources\JourneyPlanner%7cSendforapproval.json")]
        public void TestRelativePath(string fullPath, string basePath, string expectedRelativePath)
        {
            // Test non-windows paths if on other platforms
            if (Path.DirectorySeparatorChar != '\\')
            {
                fullPath = fullPath.Replace('\\', '/');
                basePath = basePath.Replace('\\', '/');
                expectedRelativePath = expectedRelativePath.Replace('\\', '/');
            }
            Assert.AreEqual(expectedRelativePath, Utilities.GetRelativePath(basePath, fullPath));
        }


        // Verify regression from
        // https://github.com/microsoft/PowerApps-Language-Tooling/issues/153
        [TestMethod]
        public void Regression153()
        {
            // Not escaped.
            var path = @"DataSources\JourneyPlanner|Sendforapproval.json";
            var escape = Utilities.EscapeFilename(path);

            var original = Utilities.UnEscapeFilename(escape);

            Assert.AreEqual(path, original);
        }

        [DataTestMethod]
        [DataRow("Long*Control*Name*Truncation*Tests***", "Long%2aControl%2aName%2aTruncation%2aTests%2a_959")]
        [DataRow("TestReallyLoooooooooooooooooooooooooooooooooooongControlName", "TestReallyLooooooooooooooooooooooooooooooooooo_cad")]
        [DataRow("TestControlName", "TestControlName")]
        public void TestControlNameTruncation(string originalName, string expectedName)
        {
            var truncatedName = Utilities.TruncateNameIfTooLong(originalName);
            Assert.AreEqual(truncatedName, expectedName);
        }
    }
}
