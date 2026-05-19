// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PAModelTests;

// Test that a series of .msapps can successfully roundtrip.
[TestClass]
public class RoundtripTests
{
    private static IEnumerable<object[]> TestAppFilePaths
    {
        get
        {
            var appsDirectory = new DirectoryInfo("Apps");
            foreach (var file in appsDirectory.EnumerateFiles("*.msapp", SearchOption.AllDirectories))
            {
                var testAppRelativePath = file.FullName.Substring(Environment.CurrentDirectory.Length + 1);

                yield return new object[] { testAppRelativePath };
            }
        }
    }

    // Apps live in the "Apps" folder, and should have a build action of "Copy to output"
    [TestMethod]
    [DynamicData(nameof(TestAppFilePaths))]
    public void StressTestApps(string msappPath)
    {
        var msappFullPath = Path.GetFullPath(msappPath);
        MsAppTest.StressTest(msappFullPath).Should().BeTrue();

        // If this fails, to debug it, rerun and set a breakpoint in DebugChecksum().
        MsAppTest.TestClone(msappFullPath).Should().BeTrue();
    }
}
