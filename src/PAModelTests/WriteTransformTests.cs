// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace PAModelTests
{
    [TestClass]
    public class WriteTransformTests
    {
        [DataTestMethod]
        [DataRow("EmptyTestCase.msapp")]
        public void TestResourceNullCase(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            // explicitly setting it to null
            msapp._resourcesJson = null;

            msapp.ApplyBeforeMsAppWriteTransforms(errors);
            Assert.IsFalse(errors.HasErrors);
        }

        [DataTestMethod]
        [DataRow("AccountPlanReviewerMaster.msapp")]
        public void TestAssetFilesNullCase(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            // explicitly setting it to null
            msapp._assetFiles = null;

            msapp.ApplyBeforeMsAppWriteTransforms(errors);
            Assert.IsFalse(errors.HasErrors);
        }

        [DataTestMethod]
        [DataRow("AccountPlanReviewerMaster.msapp")]
        public void TestResourcesInResourcesJsonIsNullWhenRestoringAssetFilePaths(string filename)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            msapp._resourcesJson = new ResourcesJson() { Resources = null };

            msapp.ApplyBeforeMsAppWriteTransforms(errors);
            Assert.IsFalse(errors.HasErrors);
        }
    }
}
