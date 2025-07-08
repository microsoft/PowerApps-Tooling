// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IO;
using Microsoft.PowerPlatform.Formulas.Tools.IR;

namespace PAModelTests;

// Control Template Tests
[TestClass]
public class TemplateStoreTests
{
    // Validate that the host control template hostType value is stored in entropy while unpacking
    // This example app has different host control instances with different template values like HostType
    [TestMethod]
    [DataRow("SharepointAppWithHostControls.msapp")]
    public void TestHostControlInstancesWithHostType(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        Assert.IsTrue(msapp._templateStore.Contents.ContainsKey("Host"));
        Assert.IsTrue(msapp._templateStore.Contents.ContainsKey("SharePointIntegration"));

        Assert.IsTrue(msapp._screens.TryGetValue("App", out var app));
        var sourceFile = IRStateHelpers.CombineIRAndState(app, errors, msapp._editorStateStore, msapp._templateStore, new UniqueIdRestorer(msapp._entropy), msapp._entropy);

        Assert.AreEqual("Host", sourceFile.Value.TopParent.Children.First().Name);
        Assert.AreEqual("SharePointIntegration", sourceFile.Value.TopParent.Children.Last().Name);

        // Checking if the HostType entry added
        Assert.IsTrue(sourceFile.Value.TopParent.Children.Last().Template.ExtensionData.ContainsKey("HostType"));

        // Repack the app and validate it matches the initial msapp
        using (var tempFile = new TempFile())
        {
            MsAppSerializer.SaveAsMsApp(msapp, tempFile.FullPath, new ErrorContainer());
            Assert.IsTrue(MsAppTest.Compare(root, tempFile.FullPath, Console.Out));
        }
    }

    // Validate a modern control that has a dynamic template.
    // The template has a valid template name, but makes reference to another template.
    // This example app has two modern controls (combobox and dropdown) that make reference to the same template.
    [TestMethod]
    [DataRow("ComboboxDropdown.msapp")]
    public void TestModernControlWithDynamicTemplate(string appName)
    {
        //arrange
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root), "MSAPP not found");

        //act
        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        //assert
        Assert.IsTrue(msapp._templateStore.Contents.ContainsKey("PowerApps_CoreControls_DropdownCanvasTemplate_dataField"));
        Assert.IsTrue(msapp._templateStore.Contents.ContainsKey("PowerApps_CoreControls_ComboboxCanvasTemplate_dataField"));

        // Repack the app and validate it matches the initial msapp
        using (var tempFile = new TempFile())
        {
            MsAppSerializer.SaveAsMsApp(msapp, tempFile.FullPath, new ErrorContainer());
            Assert.IsTrue(MsAppTest.Compare(root, tempFile.FullPath, Console.Out));
        }
    }
}
