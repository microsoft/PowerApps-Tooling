// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IO;
using Microsoft.PowerPlatform.Formulas.Tools.Schemas;

namespace PAModelTests;

[TestClass]
public class EntropyTests
{
    [TestMethod]
    [DataRow("ComponentTest.msapp", true)]
    [DataRow("ComponentWithSameParam.msapp", false)]
    public void TestFunctionParameters(string filename, bool invariantScriptsOnInstancesExist)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        if (invariantScriptsOnInstancesExist)
        {
            Assert.IsNotNull(msapp._entropy.FunctionParamsInvariantScriptsOnInstances);
        }

        using (var tempDir = new TempDir())
        {
            var outSrcDir = tempDir.Dir;

            // Save to sources
            // Also tests repacking, errors captured if any
            var errorSources = msapp.SaveToSources(outSrcDir);
            errorSources.ThrowOnErrors();
        }
    }

    [TestMethod]
    [DataRow("AnimationControlIdIsGuid.msapp")]
    public void TestControlIdGuidParsing(string filename)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        Assert.IsNotEmpty(msapp._entropy.ControlUniqueGuids);
        Assert.IsEmpty(msapp._entropy.ControlUniqueIds);
    }

    [TestMethod]
    [DataRow("AppWithLabel.msapp")]
    public void TestControlIdIntParsing(string filename)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        Assert.IsNotEmpty(msapp._entropy.ControlUniqueIds);
        Assert.IsEmpty(msapp._entropy.ControlUniqueGuids);
    }

    // Validate that the control template fields OverridaleProperties and PCFDynamicSchemaForIRRetrieval are stored in entropy while unpacking
    // The test app contains control instances with same template but different fields
    [TestMethod]
    [DataRow("ControlInstancesWithDifferentTemplateFields.msapp")]
    public void TestControlInstancesWithSameTemplateDifferentFields(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        Assert.IsNotEmpty(msapp._entropy.OverridablePropertiesEntry);
        Assert.IsNotEmpty(msapp._entropy.PCFDynamicSchemaForIRRetrievalEntry);
    }

    [TestMethod]
    [DataRow("AnimationControlIdIsGuid.msapp")]
    public void TestGetResourcesJSONIndicesKeyNullException(string filename)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", filename);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        // passing null resource in resourcesJson
        msapp._resourcesJson = new ResourcesJson() { Resources = new ResourceJson[] { null } };

        TransformResourceJson.PersistOrderingOfResourcesJsonEntries(msapp);
    }

    // Validate that the pcf control template is stored in entropy while unpacking
    // The test app contains control instances with same template but different fields
    [TestMethod]
    [DataRow("PcfTemplates.msapp")]
    public void TestPCFControlInstancesWithSameTemplateDifferentFields(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        Assert.IsNotEmpty(msapp._entropy.PCFTemplateEntry);
    }

    [TestMethod]
    [DataRow("AnimationControlIdIsGuid.msapp")]
    public void TestAppWithNoPCFControlInstances(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        Assert.IsEmpty(msapp._entropy.PCFTemplateEntry);
    }

    // Validate that a PCF control will still resolve its template by falling back to
    // the template store if the control's specific template isn't in Entropy.
    [TestMethod]
    [DataRow("PcfTemplates.msapp")]
    public void TestPCFControlWillFallBackToControlTemplate(string appName)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Apps", appName);
        Assert.IsTrue(File.Exists(root));

        (var msapp, var errors) = CanvasDocument.LoadFromMsapp(root);
        errors.ThrowOnErrors();

        Assert.IsNotEmpty(msapp._entropy.PCFTemplateEntry);

        // Clear out the PCF templates in entropy
        msapp._entropy.PCFTemplateEntry.Clear();
        Assert.IsEmpty(msapp._entropy.PCFTemplateEntry);

        // Repack the app and validate it matches the initial msapp
        using (var tempFile = new TempFile())
        {
            MsAppSerializer.SaveAsMsApp(msapp, tempFile.FullPath, new ErrorContainer());
            Assert.IsTrue(MsAppTest.Compare(root, tempFile.FullPath, Console.Out));
        }
    }
}
