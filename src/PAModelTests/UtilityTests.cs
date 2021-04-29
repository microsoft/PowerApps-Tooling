// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace PAModelTests
{
    [TestClass]
    public class UtilityTests
    {
        [DataTestMethod]
        [DataRow("\r\t!$^%/\\", "%0d%09%21%24%5e%25%2f%5c")]
        [DataRow("\u4523", "%%4523")]
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
        [DataRow(@"C:\Temp\DataSources\JourneyPlannerSendforapproval.json", @"C:\Temp", @"DataSources\JourneyPlannerSendforapproval.json")]
        [DataRow(@"C:\Temp\DataSources\JourneyPlanner%7cSendforapproval.json", @"C:\Temp", @"DataSources\JourneyPlanner%7cSendforapproval.json")]
        [DataRow(@"C:\Temp\MySolution\MySolution.Project\DataSources\JourneyPlanner%7cSendforapproval.json", @"C:\Temp\MySolution\MySolution.Project", @"DataSources\JourneyPlanner%7cSendforapproval.json")]
        public void TestRelativePath(string fullPath, string basePath, string expectedRelativePath)
        {
            // The 'Utilities.AppendDirectorySeparatorChar' method (which the 'Utilities.GetRelativePath' depends on)
            // has some logic that is better able to determine if a '\' is necessary for a directory based on the existence of the file/directory.
            // So we need to create the file here for the updated logic to work.
            // If the file/directory is not created then the logic falls back to the default logic

            if (!File.Exists(fullPath))
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.Create(fullPath);
            }

            // Test non-windows paths if on other platforms
            if (Path.DirectorySeparatorChar != '\\')
            {
                fullPath = fullPath.Replace('\\', '/');
                basePath = basePath.Replace('\\', '/');
                expectedRelativePath = expectedRelativePath.Replace('\\', '/');
            }

            Assert.AreEqual(expectedRelativePath, Utilities.GetRelativePath(fullPath, basePath));
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
    }
}
