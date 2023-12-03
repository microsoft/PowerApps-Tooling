// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

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
        
        foreach (var root in Directory.GetFiles(directory))
        {
            try
            {
                bool ok = MsAppTest.StressTest(root);
                Assert.IsTrue(ok);

                var cloneOk = MsAppTest.TestClone(root);
                // If this fails, to debug it, rerun and set a breakpoint in DebugChecksum().
                Assert.IsTrue(cloneOk, $"Clone failed: " + root);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }            
    }
}
