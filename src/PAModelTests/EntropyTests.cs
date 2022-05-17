// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace PAModelTests
{
    [TestClass]
    public class EntropyTests
    {
        [DataTestMethod]
        [DataRow("ComponentTest.msapp")]
        public void TestFunctionParameterOnInstances(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsNotNull(msapp._entropy.FunctionParamsInvariantScriptsOnInstances);

            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;

                // Save to sources
                // Also tests repacking, errors captured if any
                ErrorContainer errorSources = msapp.SaveToSources(outSrcDir);
                errorSources.ThrowOnErrors();
            }
        }
    }
}
