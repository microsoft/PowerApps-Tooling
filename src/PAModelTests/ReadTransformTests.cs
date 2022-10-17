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
    public class ReadTransformTests
    {  
        [DataTestMethod]
        [DataRow("GalleryTemplateNullChildren.msapp", false, false)]
        [DataRow("TestStepWithInvalidScreen.msapp", false, true)]
        [DataRow("GroupControlStateEmpty.msapp", false, true)]
        public void ApplyAfterMsAppLoadTransforms_Test(string filename, bool hasErrors, bool hasWarnings)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(path));

            // ApplyAfterMsAppLoadTransforms is called in LoadFromMsapp
            (var msapp, var errorContainer) = CanvasDocument.LoadFromMsapp(path);
            errorContainer.ThrowOnErrors();

            Assert.AreEqual(errorContainer.HasErrors, hasErrors);
            Assert.AreEqual(errorContainer.HasWarnings, hasWarnings);
        }

        [TestMethod]
        public void TestAssetFileCollision()
        {
            var doc = new CanvasDocument();

            var resource1 = new ResourceJson()
            {
                Name = "Image",
                Path = "Assets\\Images\\Image.png",
                FileName = "Image.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };
            doc._assetFiles.Add(new FilePath("Images", "Image.png"), new FileEntry());

            // passing null resource in resourcesJson
            doc._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[] { resource1, null } };

            var errorContainer = new ErrorContainer();
            Assert.ThrowsException<NullReferenceException>(() => doc.StabilizeAssetFilePaths(errorContainer));
        }
    }
}
