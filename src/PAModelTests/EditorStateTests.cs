// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.EditorState;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace PAModelTests
{
    [TestClass]
    public class EditorStateTests
    {
        private const string EditorStateFileExtension = ".editorstate.json";

        /// <summary>
        /// Tests that the top parent is given the `IsTopParent` flag.
        /// No other controls should have this flag.
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
                string outSrcDir = tempDir.Dir;

                // Save to sources
                msapp.SaveToSources(outSrcDir);

                // Go find the source file for the editor state
                string filename = $"{topParentName}{EditorStateFileExtension}";
                string fullFilePath = Path.Combine(outSrcDir, "Src", "EditorState", filename);
                if (File.Exists(fullFilePath))
                {
                    // Get the file for the specific control we're looking for
                    DirectoryReader.Entry file = new DirectoryReader.Entry(fullFilePath);
                    Dictionary<string, ControlState> editorState = file.ToObject<Dictionary<string, ControlState>>();

                    // Check that the IsTopParent was set on the correct control
                    foreach (var control in editorState)
                    {
                        if (control.Value.Name.Equals(topParentName))
                        {
                            Assert.IsTrue(control.Value.IsTopParent ?? false);
                        }
                        else
                        {
                            Assert.IsNull(control.Value.IsTopParent);
                        }
                    }
                }
                else
                {
                    Assert.Fail($"Could not find expected file {fullFilePath}.");
                }
            }
        }

        /// <summary>
        /// Tests that, when the `IsTopParent` flag is present during deserialization,
        /// the name of that control is used as the TopParentName
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
                string outSrcDir = tempDir.Dir;

                // Save to sources
                msapp.SaveToSources(outSrcDir);

                // Go find the source file for the editor state
                string filename = $"{topParentName}.editorstate.json";
                string fullFilePath = Path.Combine(outSrcDir, "Src", "EditorState", filename);
                if (File.Exists(fullFilePath))
                {
                    // Get the file for the specific control we're looking for
                    DirectoryReader.Entry file = new DirectoryReader.Entry(fullFilePath);
                    Dictionary<string, ControlState> editorState = file.ToObject<Dictionary<string, ControlState>>();

                    // Rename the file so we know that the file name itself wasn't
                    // used but rather than correct control name.
                    string newFileName = Guid.NewGuid().ToString();
                    string newFilePath = Path.Combine(outSrcDir, "Src", "EditorState", $"{newFileName}{EditorStateFileExtension}");

                    File.Move(fullFilePath, newFilePath);

                    // Load app from the source folder
                    var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

                    // Find the relevant controls and check their top parent name
                    foreach (var control in editorState)
                    {
                        app._editorStateStore.TryGetControlState(control.Value.Name, out ControlState state);
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
        /// Tests that, when the `IsTopParent` flag is not present during
        /// deserialization, the name of the file is used as the TopParentName.
        /// This preserves backwards compatability for apps packed prior to
        /// changing how the TopParentName was read.
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
                string outSrcDir = tempDir.Dir;

                // Save to sources
                msapp.SaveToSources(outSrcDir);

                // Go find the source file for the editor state
                string filename = $"{topParentName}.editorstate.json";
                string fullFilePath = Path.Combine(outSrcDir, "Src", "EditorState", filename);
                if (File.Exists(fullFilePath))
                {
                    // Get the file for the specific control we're looking for
                    DirectoryReader.Entry file = new DirectoryReader.Entry(fullFilePath);
                    Dictionary<string, ControlState> editorState = file.ToObject<Dictionary<string, ControlState>>();

                    // Update the file to remove the IsTopParent
                    string updatedContent = File.ReadAllText(fullFilePath);
                    updatedContent = updatedContent.Replace("\"IsTopParent\": true,", string.Empty);

                    // Rename the file so we know that the file name itself wasn't
                    // used but rather than correct control name.
                    string newFileName = Guid.NewGuid().ToString();
                    string newFilePath = Path.Combine(outSrcDir, "Src", "EditorState", $"{newFileName}{EditorStateFileExtension}");

                    File.WriteAllText(newFilePath, updatedContent);

                    // Remove the old file, we only want the re-written and re-named file
                    File.Delete(fullFilePath);

                    // Load app from the source folder
                    var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

                    // Find the relevant controls and check their top parent name
                    foreach (var control in editorState)
                    {
                        app._editorStateStore.TryGetControlState(control.Value.Name, out ControlState state);
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
}
