// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.IO;

namespace PAModelTests;

[TestClass]
public class ReadTransformTests
{
    [TestMethod]
    [DataRow("GalleryTemplateNullChildren.msapp", false, false)]
    [DataRow("TestStepWithInvalidScreen.msapp", false, true)]
    [DataRow("GroupControlStateEmpty.msapp", false, true)]
    public void ApplyAfterMsAppLoadTransformsTest(string filename, bool hasErrors, bool hasWarnings)
    {
        var path = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
        Assert.IsTrue(File.Exists(path));

        // ApplyAfterMsAppLoadTransforms is called in LoadFromMsapp
        (_, var errorContainer) = CanvasDocument.LoadFromMsapp(path);
        errorContainer.ThrowOnErrors();

        Assert.AreEqual(errorContainer.HasErrors, hasErrors);
        Assert.AreEqual(errorContainer.HasWarnings, hasWarnings);
    }

    [TestMethod]
    public void TestNullResource()
    {
        var doc = new CanvasDocument();

        // resource name null case
        var resource1 = new ResourceJson()
        {
            Name = null,
            Path = "Assets\\Images\\Image.png",
            FileName = "Image.png",
            ResourceKind = ResourceKind.LocalFile,
            Content = ContentKind.Image,
        };
        doc._assetFiles.Add(new FilePath("Images", "Image.png"), new FileEntry());

        // passing null resource in resourcesJson
        doc._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[] { resource1, null } };

        var errorContainer = new ErrorContainer();
        doc.StabilizeAssetFilePaths(errorContainer);

        Assert.AreEqual(errorContainer.HasErrors, false);
    }
}
