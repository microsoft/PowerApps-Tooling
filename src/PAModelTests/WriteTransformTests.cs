// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IO;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;

namespace PAModelTests;

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

    [DataTestMethod]
    [DataRow("AccountPlanReviewerMaster.msapp")]
    public void TestNullExceptionInRestoreAssetsFilePathsIsLoggedAsAnInternalError(string filename)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        // explicitly setting it to null
        msapp._localAssetInfoJson[msapp._resourcesJson.Resources.Last().FileName] = null;


        msapp.ApplyBeforeMsAppWriteTransforms(errors);
        Assert.IsTrue(errors.HasErrors);
        Assert.IsNotNull(errors.FirstOrDefault(error => error.Code == ErrorCode.InternalError));
    }

    [DataTestMethod]
    [DataRow("AccountPlanReviewerMaster.msapp")]
    public void TestNullExceptionInGetMsAppFilesIsLoggedAsAnInternalError(string filename)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        using var sourcesTempDir = new TempDir();
        var sourcesTempDirPath = sourcesTempDir.Dir;
        msapp.SaveToSources(sourcesTempDirPath);

        var loadFromSourceErrors = new ErrorContainer();
        var loadedMsApp = SourceSerializer.LoadFromSource(sourcesTempDirPath, loadFromSourceErrors);
        loadFromSourceErrors.ThrowOnErrors();

        // explicitly setting it to null
        loadedMsApp._header = null;
        using var tempFile = new TempFile();
        var saveToMsAppErrors = loadedMsApp.SaveToMsApp(tempFile.FullPath);

        Assert.IsTrue(saveToMsAppErrors.HasErrors);
        Assert.IsNotNull(saveToMsAppErrors.FirstOrDefault(error => error.Code == ErrorCode.InternalError));
    }
}
