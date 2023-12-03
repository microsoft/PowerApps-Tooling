// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace PAModelTests;

// Control Template Tests
[TestClass]
public class TemplateStoreTests
{
    // Validate that the host control template hostType value is stored in entropy while unpacking
    // This example app has different host control instances with different template values like HostType
    [DataTestMethod]
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
}
