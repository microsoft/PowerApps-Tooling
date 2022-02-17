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
                    EditorStateFile editorStateFile = file.ToObject<EditorStateFile>();

                    // Check that the IsTopParent was set correctly
                    Assert.AreEqual(topParentName, editorStateFile.TopParentName);
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
                    EditorStateFile editorStateFile = file.ToObject<EditorStateFile>();

                    // Rename the file so we know that the file name itself wasn't
                    // used but rather than correct control name.
                    string newFileName = Guid.NewGuid().ToString();
                    string newFilePath = Path.Combine(outSrcDir, "Src", "EditorState", $"{newFileName}{EditorStateFileExtension}");

                    File.Move(fullFilePath, newFilePath);

                    // Load app from the source folder
                    var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

                    // Find the relevant controls and check their top parent name
                    foreach (var control in editorStateFile.ControlStates)
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
        /// Test that, when deserializing an older file format for the editor
        /// state, the name of the file is used as the `TopParentName` and the
        /// controls are deserialized correctly.
        /// 
        /// This preserves backwards compatability for apps packed prior to
        /// changing how the `TopParentName` was stored and read.
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
                    EditorStateFile editorStateFile = file.ToObject<EditorStateFile>();

                    // Rename the file so we know that the file name itself is used.
                    string newFileName = Guid.NewGuid().ToString();
                    string newFilePath = Path.Combine(outSrcDir, "Src", "EditorState");

                    // Write out only the dictionary to the file, which is the older format.
                    DirectoryWriter dir = new DirectoryWriter(newFilePath);
                    dir.WriteAllJson(newFilePath, $"{newFileName}{EditorStateFileExtension}", editorStateFile.ControlStates);

                    // Remove the old file, we only want the re-written and re-named file
                    File.Delete(fullFilePath);

                    // Load app from the source folder
                    var app = SourceSerializer.LoadFromSource(outSrcDir, new ErrorContainer());

                    // Find the relevant controls and check their top parent name
                    foreach (var control in editorStateFile.ControlStates)
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
