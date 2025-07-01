// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace PAModelTests;

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
}
