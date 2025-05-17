// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;

namespace PAModelTests;

// Test that a series of .msapps can successfully roundtrip.
[TestClass]
public class RoundtripTests
{
    private static IEnumerable<object[]> TestAppFilePaths => Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Apps"), "*.msapp").Select(p => new[] { p });

    // Apps live in the "Apps" folder, and should have a build action of "Copy to output"
    [TestMethod]
    [DynamicData(nameof(TestAppFilePaths))]
    public void StressTestApps(string msappPath)
    {
        MsAppTest.StressTest(msappPath).Should().BeTrue();

        // If this fails, to debug it, rerun and set a breakpoint in DebugChecksum().
        MsAppTest.TestClone(msappPath).Should().BeTrue();
    }
}
