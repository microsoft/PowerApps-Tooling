// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.PowerPlatform.Formulas.Tools;

namespace PAModelTests;

// Test that a series of .msapps can successfully roundtrip.
[TestClass]
public class RoundtripTests
{
    // Apps live in the "Apps" folder, and should have a build action of "Copy to output"
    [TestMethod]
    public void StressTestApps()
    {
        var directory = Path.Combine(Environment.CurrentDirectory, "Apps");

        Parallel.ForEach(Directory.GetFiles(directory), root =>
        {
            try
            {
                var ok = MsAppTest.StressTest(root);
                Assert.IsTrue(ok);

                var cloneOk = MsAppTest.TestClone(root);
                // If this fails, to debug it, rerun and set a breakpoint in DebugChecksum().
                Assert.IsTrue(cloneOk, $"Clone failed: " + root);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        });
    }
}
