// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
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

        // Validate that a PCF control will still resolve its template by falling back to
        // the template store if the control's specific template isn't in Entropy.
        [DataTestMethod]
        [DataRow("PcfTemplates.msapp")]
        public void TestPCFControlWillFallBackToControlTemplate(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.PCFTemplateEntry.Count > 0);

            // Clear out the PCF templates in entropy
            msapp._entropy.PCFTemplateEntry.Clear();
            Assert.IsTrue(msapp._entropy.PCFTemplateEntry.Count == 0);

            // Repack the app and validate it matches the initial msapp
            using (var tempFile = new TempFile())
            {
                MsAppSerializer.SaveAsMsApp(msapp, tempFile.FullPath, new ErrorContainer());
                Assert.IsTrue(MsAppTest.Compare(root, tempFile.FullPath, Console.Out));
            }
        }

        // Validate that the host control template hostType value is stored in entropy while unpacking
        [DataTestMethod]
        [DataRow("HostControlTestWithHostType.msapp")]
        public void TestHostControlWithHostType(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.HostTypeEntry.Count > 0);

            Assert.IsTrue(msapp._screens.TryGetValue("App", out var app));
            var sourceFile = IRStateHelpers.CombineIRAndState(app, errors, msapp._editorStateStore, msapp._templateStore, new UniqueIdRestorer(msapp._entropy), msapp._entropy);

            Assert.AreEqual("Host1", sourceFile.Value.TopParent.Children.First().Name);
            Assert.AreEqual("Host2", sourceFile.Value.TopParent.Children.Last().Name);

            // Checking if the HostType Entry is added
            Assert.IsTrue(sourceFile.Value.TopParent.Children.Last().Template.ExtensionData.ContainsKey(IRStateHelpers.ControlTemplateHostTypePropertyName));
            Assert.IsFalse(sourceFile.Value.TopParent.Children.Last().Template.ExtensionData.ContainsKey(IRStateHelpers.ControlTemplateHostServicePropertyName));
        }

        [DataTestMethod]
        [DataRow("HostControlTestWithHostTypeAndHostService.msapp")]
        public void TestHostControlWithHostTypeAndService(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.HostTypeEntry.Count > 0);
            Assert.IsTrue(msapp._entropy.HostServiceEntry.Count > 0);

            Assert.IsTrue(msapp._screens.TryGetValue("App", out var app));
            var sourceFile = IRStateHelpers.CombineIRAndState(app, errors, msapp._editorStateStore, msapp._templateStore, new UniqueIdRestorer(msapp._entropy), msapp._entropy);

            Assert.AreEqual("Host1", sourceFile.Value.TopParent.Children.First().Name);
            Assert.AreEqual("Host2", sourceFile.Value.TopParent.Children.Last().Name);

            // Checking if the HostType and HostService Entries were added
            Assert.IsTrue(sourceFile.Value.TopParent.Children.Last().Template.ExtensionData.ContainsKey(IRStateHelpers.ControlTemplateHostTypePropertyName));
            Assert.IsTrue(sourceFile.Value.TopParent.Children.Last().Template.ExtensionData.ContainsKey(IRStateHelpers.ControlTemplateHostServicePropertyName));
        }

        [DataTestMethod]
        [DataRow("HostControlTestWithHostType.msapp")]
        public void TestHostControlWithHostTypeFailsWithNoEntropyEntry(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.HostTypeEntry.Count > 0);

            // Clear out the HostType entropy entries
            msapp._entropy.HostTypeEntry.Clear();
            Assert.IsTrue(msapp._entropy.HostTypeEntry.Count == 0);

            // Repack the app and validate that it does not matches the initial msapp
            using (var tempFile = new TempFile())
            {
                MsAppSerializer.SaveAsMsApp(msapp, tempFile.FullPath, new ErrorContainer());
                Assert.IsFalse(MsAppTest.Compare(root, tempFile.FullPath, Console.Out));
            }
        }

        [DataTestMethod]
        [DataRow("HostControlTestWithHostTypeAndHostService.msapp")]
        public void TestHostControlWithHostTypeAndServiceWithNoEntropyEntries(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._entropy.HostTypeEntry.Count > 0);
            Assert.IsTrue(msapp._entropy.HostServiceEntry.Count > 0);

            // Clear out the HostType and HostService entropy entries
            msapp._entropy.HostTypeEntry.Clear();
            Assert.IsTrue(msapp._entropy.HostTypeEntry.Count == 0);

            msapp._entropy.HostServiceEntry.Clear();
            Assert.IsTrue(msapp._entropy.HostServiceEntry.Count == 0);

            // Repack the app and validate it does not match the initial msapp
            using (var tempFile = new TempFile())
            {
                MsAppSerializer.SaveAsMsApp(msapp, tempFile.FullPath, new ErrorContainer());
                Assert.IsFalse(MsAppTest.Compare(root, tempFile.FullPath, Console.Out));
            }
        }
    }
}
