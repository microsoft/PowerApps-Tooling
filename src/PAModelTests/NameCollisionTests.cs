// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace PAModelTests
{
    [TestClass]
    public class NameCollisionTests
    {

        [TestMethod]
        public void TestAssetFileRename()
        {
            var doc = new CanvasDocument();
            var resource1 = new ResourceJson()
            {
                Name = "Image", // Capital
                Path = "Assets\\Images\\Image.png",
                FileName = "Image.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };

            doc._assetFiles.Add(new FilePath("Images", "Image.png"), new FileEntry());

            var resource2 = new ResourceJson()
            {
                Name = "image", // Lowercase
                Path = "Assets\\Images\\image.png",
                FileName = "image.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };

            doc._assetFiles.Add(new FilePath("Images", "image.png"), new FileEntry());

            var resource3 = new ResourceJson()
            {
                Name = "image_1",
                Path = "Assets\\Images\\image_1.png",
                FileName = "image_1.png",
                ResourceKind = ResourceKind.LocalFile,
                Content = ContentKind.Image,
            };

            doc._assetFiles.Add(new FilePath("Images", "image_1.png"), new FileEntry());

            doc._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[] { resource1, resource2, resource3 } };

            var errorContainer = new ErrorContainer();
            doc.StabilizeAssetFilePaths(errorContainer);

            var newFileNames = doc._resourcesJson.Resources.Select(resource => resource.Name);
            Assert.IsTrue(newFileNames.Contains("Image"));
            Assert.IsTrue(newFileNames.Contains("image_1"));
            Assert.IsTrue(newFileNames.Contains("image_2"));
        }
    }
}
