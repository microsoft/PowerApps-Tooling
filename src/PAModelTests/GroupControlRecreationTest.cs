// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace PAModelTests
{
    [TestClass]
    public class GroupControlRecreationTest
    {
        // Test that group controls are moved to the end of the list when the editorstate is removed. 
        [TestMethod]
        public void TestGroupControlRecreation()
        {
            // Pull both the msapp and the baseline from our embedded resources. 
            var root = Path.Combine(Environment.CurrentDirectory, "Apps", "GroupControlTest.msapp");
            Assert.IsTrue(File.Exists(root));

            (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
            errors.ThrowOnErrors();

            msapp._editorStateStore.Remove("Group1");

            msapp.ApplyBeforeMsAppWriteTransforms(errors);
            errors.ThrowOnErrors();

            Assert.IsTrue(msapp._screens.TryGetValue("Screen1", out var screen));

            var sourceFile = IRStateHelpers.CombineIRAndState(screen, errors, msapp._editorStateStore, msapp._templateStore, new UniqueIdRestorer(msapp._entropy), msapp._entropy);

            Assert.AreEqual("Screen1", sourceFile.ControlName);

            // Check that the group control was still moved to the end of the children list
            Assert.AreEqual("Group1", sourceFile.Value.TopParent.Children.Last().Name);
        }
        [DataTestMethod]
        [DataRow("GroupControlTest.msapp")]
        public void TestGroupControlsState_Empty(string filename)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
            Assert.IsTrue(File.Exists(path));

            // ApplyAfterMsAppLoadTransforms is called in LoadFromMsapp
            (var msapp, var errorContainer) = CanvasDocument.LoadFromMsapp(path);
            errorContainer.ThrowOnErrors();           

            var errors = new ErrorContainer();
            using (var tempDir = new TempDir())
            {
                string outSrcDir = tempDir.Dir;
                if (msapp._screens.TryGetValue("Screen1", out var screen))
                {
                    screen.Children[0].Children.Clear();
                }

                // Save to sources
                msapp.SaveToSources(outSrcDir);                

                // Load app from the sources after deleting the entropy
                var app = SourceSerializer.LoadFromSource(outSrcDir, errors);

                using (var tempFile = new TempFile())
                {
                    errors = app.SaveToSources(outSrcDir, path);
                }
            }


            Assert.IsTrue(errors.ToString().Contains("Warning PA2002: Validation issue: Group control state is empty for Group1"));
        }
    }    
}
