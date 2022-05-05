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
    public class ReadTransformTests
    {   
        [DataTestMethod]
        [DataRow("GalleryTemplateNullChildren.msapp")]
        public void AfterReadNullChildren(string filename)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(path));

            (var msapp, var errorContainer) = CanvasDocument.LoadFromMsapp(path);
            errorContainer.ThrowOnErrors();
            msapp.ApplyAfterMsAppLoadTransforms(errorContainer);
            Assert.IsFalse(errorContainer.HasErrors);
        }

        [DataTestMethod]
        [DataRow("TestStepWithInvalidScreen.msapp")]
        public void TestStepWithInvalidScreenAfterRead(string filename)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(path));

            (var msapp, var errorContainer) = CanvasDocument.LoadFromMsapp(path);
            errorContainer.ThrowOnErrors();
            msapp.ApplyAfterMsAppLoadTransforms(errorContainer);

            // Test step with invalid screen is no more an error, but warning thrown
            // Logic handled in AppTestsTransform.cs
            Assert.IsFalse(errorContainer.HasErrors);
            Assert.IsTrue(errorContainer.HasWarnings);
        }
    }
}
