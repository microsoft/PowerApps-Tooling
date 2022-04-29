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
    }
}
