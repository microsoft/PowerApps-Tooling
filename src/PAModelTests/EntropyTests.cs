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
        [DataRow("ComponentTest.msapp", true)]
        [DataRow("ComponentWithSameParam.msapp", false)]
        public void TestFunctionParameters(string filename, bool invariantScriptsOnInstancesExist)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            if (invariantScriptsOnInstancesExist)
            {
                Assert.IsNotNull(msapp._entropy.FunctionParamsInvariantScriptsOnInstances);
            }

            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;

                // Save to sources
                // Also tests repacking, errors captured if any
                ErrorContainer errorSources = msapp.SaveToSources(outSrcDir);
                errorSources.ThrowOnErrors();
            }
        }

        [DataTestMethod]
        [DataRow("AnimationControlIdIsGuid.msapp")]
        public void TestControlIdGuidParsing(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.ControlUniqueGuids.Count > 0);
            Assert.AreEqual(msapp._entropy.ControlUniqueIds.Count, 0);
        }

        [DataTestMethod]
        [DataRow("AppWithLabel.msapp")]
        public void TestControlIdIntParsing(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.ControlUniqueIds.Count > 0);
            Assert.AreEqual(msapp._entropy.ControlUniqueGuids.Count, 0);
        }
    }
}
