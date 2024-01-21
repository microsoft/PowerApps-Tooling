// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IO;

namespace PAModelTests;

// DataSources Tests
[TestClass]
public class AppTestsTest
{
    // Validates that the App can be repacked after deleting the EditorState files, when the app contains app tests which refer to screens.
    [DataTestMethod]
    [DataRow("TestStudio_Test.msapp")]
    public void TestPackWhenEditorStateIsDeleted(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        using (var tempDir = new TempDir())
        {
            var outSrcDir = tempDir.Dir;

            // Save to sources
            msapp.SaveToSources(outSrcDir);

            // Delete Entropy directory
            var editorStatePath = Path.Combine(outSrcDir, "Src", "EditorState");
            if (Directory.Exists(editorStatePath))
            {
                Directory.Delete(editorStatePath, true);
            }

            // Load app from the sources after deleting the entropy
            var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

            using (var tempFile = new TempFile())
            {
                // Repack the app
                MsAppSerializer.SaveAsMsApp(app, tempFile.FullPath, new ErrorContainer());
            }
        }
    }

    // Validates that the App can be repacked after deleting the Entropy files, when the app contains app tests which refer to screens.
    [DataTestMethod]
    [DataRow("TestStudio_Test.msapp")]
    public void TestPackWhenEntropyIsDeleted(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        using (var tempDir = new TempDir())
        {
            var outSrcDir = tempDir.Dir;

            // Save to sources
            msapp.SaveToSources(outSrcDir);

            // Delete Entropy directory
            var entropyPath = Path.Combine(outSrcDir, "Entropy");
            if (Directory.Exists(entropyPath))
            {
                Directory.Delete(entropyPath, true);
            }

            // Load app from the sources after deleting the entropy
            var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

            using (var tempFile = new TempFile())
            {
                // Repack the app
                MsAppSerializer.SaveAsMsApp(app, tempFile.FullPath, new ErrorContainer());

                // re-unpack should succeed
                (var msapp1, var errors1) = CanvasDocument.LoadFromMsapp(tempFile.FullPath);
                using var tempSaveDir = new TempDir();
                msapp1.SaveToSources(tempSaveDir.Dir);
            }
        }
    }
}
