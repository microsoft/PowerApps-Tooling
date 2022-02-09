// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;
using Microsoft.PowerPlatform.Formulas.Tools.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
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

        [DataTestMethod]
        [DataRow("AppWithLabel.msapp")]
        [DataRow("DuplicateScreen.msapp")]
        public void TestScreenRename(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;

                // Create a list of expected YAML file names based on the available screens
                List<string> expectedYamlFiles = new List<string>();
                foreach (var control in msapp._screens)
                {
                    string originalScreenName = control.Key.ToLower();

                    int duplicateFileSuffix = 0;
                    string uniqueScreenName = $"{originalScreenName}.fx.yaml";
                    while (expectedYamlFiles.Contains(uniqueScreenName))
                    {
                        uniqueScreenName = $"{originalScreenName}_{++duplicateFileSuffix}.fx.yaml";
                    }

                    expectedYamlFiles.Add(uniqueScreenName);
                }

                // Save to sources
                msapp.SaveToSources(outSrcDir);

                // Look for the expected YAML files
                string srcPath = Path.Combine(outSrcDir, "Src");
                foreach (string yamlFile in Directory.GetFiles(srcPath, "*.fx.yaml", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(yamlFile).ToLower();
                    if (expectedYamlFiles.Contains(fileName))
                    {
                        expectedYamlFiles.Remove(fileName);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected file {yamlFile} in Src folder.");
                    }
                }

                // There should be no expected files that were not found
                Assert.AreEqual<int>(expectedYamlFiles.Count, 0, $"{expectedYamlFiles.Count} screens not found in Src directory.");
            }
        }

        [DataTestMethod]
        [DataRow("AppWithLabel.msapp")]
        [DataRow("DuplicateScreen.msapp")]
        [DataRow("ComponentNameCollision.msapp")]
        public void TestEditorStateRename(string appName)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;

                // Create a list of expected EditorState file names based on the available screens and components
                List<string> expectedEditorStateFiles = new List<string>();
                foreach (var control in msapp._screens.Concat(msapp._components))
                {
                    string originalEditorStateName = control.Key.ToLower();

                    int duplicateFileSuffix = 0;
                    string uniqueEditorStateName = $"{originalEditorStateName}.editorstate.json";
                    while (expectedEditorStateFiles.Contains(uniqueEditorStateName))
                    {
                        uniqueEditorStateName = $"{originalEditorStateName}_{++duplicateFileSuffix}.editorstate.json";
                    }

                    expectedEditorStateFiles.Add(uniqueEditorStateName);
                }

                // Save to sources
                msapp.SaveToSources(outSrcDir);

                // Look for the expected EditorState files
                string srcPath = Path.Combine(outSrcDir, "Src\\EditorState");
                foreach (string editorStateFile in Directory.GetFiles(srcPath, "*.editorstate.json", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(editorStateFile).ToLower();
                    if (expectedEditorStateFiles.Contains(fileName))
                    {
                        expectedEditorStateFiles.Remove(fileName);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected file {editorStateFile} in EditorState folder.");
                    }
                }

                // There should be no expected files that were not found
                Assert.AreEqual<int>(expectedEditorStateFiles.Count, 0, $"{expectedEditorStateFiles.Count} editor state files not found in EditorState directory.");
            }
        }
    }
}
