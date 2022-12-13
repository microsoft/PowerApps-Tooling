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

            Assert.IsNotNull(msapp._entropy.OverridablePropertiesEntry);
            Assert.IsNotNull(msapp._entropy.PCFDynamicSchemaForIRRetrievalEntry);
        }

        [DataTestMethod]
        [DataRow("AnimationControlIdIsGuid.msapp")]
        public void TestGetResourcesJSONIndicesKeyNullException(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            var resource1 = new ResourceJson()
            {
                Name = "Image1",
                Path = "Assets\\Images\\Image.png",
                FileName = "Image.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };
            msapp._assetFiles.Add(new FilePath("Images", "Image.png"), new FileEntry());

            // passing null resource in resourcesJson
            msapp._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[] { resource1, null } };
                        
            TranformResourceJson.PersistOrderingOfResourcesJsonEntries(msapp);
        }
    }
}
