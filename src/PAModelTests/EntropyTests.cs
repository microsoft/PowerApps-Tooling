// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
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

        // Validate that the control template fields OverridaleProperties and PCFDynamicSchemaForIRRetrieval are stored in entropy while unpacking
        // The test app contains control instances with same template but different fields
        [DataTestMethod]
        [DataRow("ControlInstancesWithDifferentTemplateFields.msapp")]
        public void TestControlInstancesWithSameTemplateDifferentFields(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.OverridablePropertiesEntry.Count > 0);
            Assert.IsTrue(msapp._entropy.PCFDynamicSchemaForIRRetrievalEntry.Count > 0);
        }

        [DataTestMethod]
        [DataRow("AnimationControlIdIsGuid.msapp")]
        public void TestGetResourcesJSONIndicesKeyNullException(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            // passing null resource in resourcesJson
            msapp._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[] { null } };
                        
            TranformResourceJson.PersistOrderingOfResourcesJsonEntries(msapp);
        }

        // Validate that the pcf control template is stored in entropy while unpacking
        // The test app contains control instances with same template but different fields
        [DataTestMethod]
        [DataRow("PcfTemplates.msapp")]
        public void TestPCFControlInstancesWithSameTemplateDifferentFields(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.PCFTemplateEntry.Count > 0);
        }

        [DataTestMethod]
        [DataRow("AnimationControlIdIsGuid.msapp")]
        public void TestAppWithNoPCFControlInstances(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.PCFTemplateEntry.Count == 0);
        }
    }
}
