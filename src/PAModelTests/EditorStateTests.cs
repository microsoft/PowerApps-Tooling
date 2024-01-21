// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.PowerPlatform.Formulas.Tools.IO;

namespace PAModelTests;

[TestClass]
public class EditorStateTests
{
    private const string EditorStateFileExtension = ".editorstate.json";

    /// <summary>
    /// Tests that the top parent name is set properly on the editor state file.
    /// </summary>
    [DataTestMethod]
    [DataRow("AppWithLabel.msapp", "Screen1")]
    [DataRow("DuplicateScreen.msapp", "Screen1")]
    public void TestTopParentSerialization(string appName, string topParentName)
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

            // Go find the source file for the editor state
            var filename = $"{topParentName}{EditorStateFileExtension}";
            var fullFilePath = Path.Combine(outSrcDir, "Src", "EditorState", filename);
            if (File.Exists(fullFilePath))
            {
                // Get the file for the specific control we're looking for
                var file = new DirectoryReader.Entry(fullFilePath);
                var editorState = file.ToObject<ControlTreeState>();

                // Check that the IsTopParent was set correctly
                Assert.AreEqual(topParentName, editorState.TopParentName);
            }
            else
            {
                Assert.Fail($"Could not find expected file {fullFilePath}.");
            }
        }
    }

    /// <summary>
    /// Tests that the `TopParentName` for each control is set to the correct
    /// value when the app is deserialized.
    /// </summary>
    [DataTestMethod]
    [DataRow("AppWithLabel.msapp", "Screen1")]
    [DataRow("DuplicateScreen.msapp", "Screen1")]
    public void TestTopParentNameLoad(string appName, string topParentName)
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

            // Go find the source file for the editor state
            var filename = $"{topParentName}.editorstate.json";
            var fullFilePath = Path.Combine(outSrcDir, "Src", "EditorState", filename);
            if (File.Exists(fullFilePath))
            {
                // Get the file for the specific control we're looking for
                var file = new DirectoryReader.Entry(fullFilePath);
                var editorState = file.ToObject<ControlTreeState>();

                // Rename the file so we know that the file name itself wasn't
                // used but rather than correct control name.
                var newFileName = Guid.NewGuid().ToString();
                var newFilePath = Path.Combine(outSrcDir, "Src", "EditorState", $"{newFileName}{EditorStateFileExtension}");

                File.Move(fullFilePath, newFilePath);

                // Load app from the source folder
                var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

                // Find the relevant controls and check their top parent name
                foreach (var control in editorState.ControlStates)
                {
                    app._editorStateStore.TryGetControlState(control.Value.Name, out var state);
                    Assert.AreEqual(topParentName, state.TopParentName);
                }
            }
            else
            {
                Assert.Fail($"Could not find expected file {fullFilePath}.");
            }
        }
    }

    /// <summary>
    /// Test that, when deserializing an older file format for the editor
    /// state, the name of the file is used as the `TopParentName` and the
    /// controls are deserialized correctly.
    /// 
    /// This preserves backwards compatability for apps packed prior to
    /// changing how the `TopParentName` was stored and read.
    /// 
    /// When SourceSerializer is updated past v24, this could be removed entirely.
    /// </summary>
    [DataTestMethod]
    [DataRow("AppWithLabel.msapp", "Screen1")]
    [DataRow("DuplicateScreen.msapp", "Screen1")]
    public void TestTopParentNameFallback(string appName, string topParentName)
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

            // Go find the source file for the editor state
            var filename = $"{topParentName}.editorstate.json";
            var fullFilePath = Path.Combine(outSrcDir, "Src", "EditorState", filename);
            if (File.Exists(fullFilePath))
            {
                // Get the file for the specific control we're looking for
                var file = new DirectoryReader.Entry(fullFilePath);
                var editorState = file.ToObject<ControlTreeState>();

                // Rename the file so we know that the file name itself is used.
                var newFileName = Guid.NewGuid().ToString();
                var newFilePath = Path.Combine(outSrcDir, "Src", "EditorState");

                // Write out only the dictionary to the file, which is the older format.
                var dir = new DirectoryWriter(newFilePath);
                dir.WriteAllJson(newFilePath, $"{newFileName}{EditorStateFileExtension}", editorState.ControlStates);

                // Remove the old file, we only want the re-written and re-named file
                File.Delete(fullFilePath);

                // Load app from the source folder
                var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

                // Find the relevant controls and check their top parent name
                foreach (var control in editorState.ControlStates)
                {
                    app._editorStateStore.TryGetControlState(control.Value.Name, out var state);
                    Assert.AreEqual(newFileName, state.TopParentName);
                }
            }
            else
            {
                Assert.Fail($"Could not find expected file {fullFilePath}.");
            }
        }
    }
}
