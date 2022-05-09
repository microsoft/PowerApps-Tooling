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
        [DataRow("GalleryTemplateNullChildren.msapp", false, false)]
        [DataRow("TestStepWithInvalidScreen.msapp", false, true)]
        public void ApplyAfterMsAppLoadTransforms_Test(string filename, bool hasErrors, bool hasWarnings)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(path));

            (var msapp, var errorContainer) = CanvasDocument.LoadFromMsapp(path);
            errorContainer.ThrowOnErrors();
            msapp.ApplyAfterMsAppLoadTransforms(errorContainer);

            Assert.AreEqual(errorContainer.HasErrors, hasErrors);
            Assert.AreEqual(errorContainer.HasWarnings, hasWarnings);
        }
    }
}
