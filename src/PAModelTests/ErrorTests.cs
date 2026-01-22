// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.IO;

namespace PAModelTests;

[TestClass]
public class ErrorTests
{
    public static string PathToValidMsapp = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
    public static string PathMissingMsapp = Path.Combine(Environment.CurrentDirectory, "Missing", "Missing.msapp");
    public static string PathMissingDir = Path.Combine(Environment.CurrentDirectory, "MissingDirectory");
    public static int counter;

    [TestMethod]
    public void OpenCorruptedMsApp()
    {
        // Empty stream is invalid document, should generate a Read error.
        using var ms = new MemoryStream();

        (var doc, var errors) = CanvasDocument.LoadFromMsapp(ms);
        errors.HasErrors.Should().BeTrue();
        doc.Should().BeNull();
    }

    [TestMethod]
    public void OpenMissingMsApp()
    {
        (var doc, var errors) = CanvasDocument.LoadFromMsapp(PathMissingMsapp);
        errors.HasErrors.Should().BeTrue();
        doc.Should().BeNull();
    }

    [TestMethod]
    public void OpenBadSources()
    {
        // Common error can be mixing up arguments. Ensure that we detect passing a valid msapp as a source param.
        File.Exists(PathToValidMsapp).Should().BeTrue();

        (var doc, var errors) = CanvasDocument.LoadFromSources(PathToValidMsapp);
        errors.HasErrors.Should().BeTrue();
        doc.Should().BeNull();
    }

    [TestMethod]
    public void OpenMissingSources()
    {
        (var doc, var errors) = CanvasDocument.LoadFromSources(PathMissingDir);
        errors.HasErrors.Should().BeTrue();
        doc.Should().BeNull();
    }

    [TestMethod]
    public void BadWriteDir()
    {
        string path = null;

        // should throw on null
        Assert.ThrowsExactly<DocumentException>(() => DirectoryWriter.EnsureFileDirExists(path));
    }

    [TestMethod]
    [DataRow("complexChanged1.json", "complexChanged2.json", "complexChangedOutput.txt")]
    [DataRow("complexAdded1.json", "complexAdded2.json", "complexAddedOutput.txt")]
    [DataRow("complexRemoved1.json", "complexRemoved2.json", "complexRemovedOutput.txt")]
    [DataRow("simpleChanged1.json", "simpleChanged2.json", "simpleChangedOutput.txt")]
    [DataRow("simpleAdded1.json", "simpleAdded2.json", "simpleAddedOutput.txt")]
    [DataRow("simpleRemoved1.json", "simpleRemoved2.json", "simpleRemovedOutput.txt")]
    [DataRow("emptyNestedArray1.json", "emptyNestedArray2.json", "emptyNestedArrayOutput.txt")]
    [DataRow("simpleArray1.json", "simpleArray2.json", "simpleArrayOutput.txt")]

    public void TestJSONValueChanged(string file1, string file2, string file3)
    {

        var path1 = Path.Combine(Environment.CurrentDirectory, "TestData", file1);
        var path2 = Path.Combine(Environment.CurrentDirectory, "TestData", file2);
        var path3 = Path.Combine(Environment.CurrentDirectory, "TestData", file3);

        var errorContainer = new ErrorContainer();

        var jsonString1 = File.ReadAllBytes(path1);
        var jsonString2 = File.ReadAllBytes(path2);

        var jsonDictionary1 = MsAppTest.FlattenJson(jsonString1);
        var jsonDictionary2 = MsAppTest.FlattenJson(jsonString2);

        // IsMismatched on mismatched files
        MsAppTest.CheckPropertyChangedRemoved(jsonDictionary1, jsonDictionary2, errorContainer, "");
        MsAppTest.CheckPropertyAdded(jsonDictionary1, jsonDictionary2, errorContainer, "");

        // Confirm that the unit tests have the expected output
        File.ReadAllText(path3).Should().Be(errorContainer.ToString());
    }

    [TestMethod]
    [DataRow("ImageApp_SwitchNames.msapp", "ImageApp.msapp")]
    [DataRow("ImageApp.msapp", "ImageApp_SwitchNames.msapp")]
    public void CompareChecksumImageNotReadAsJSONTest(string app1, string app2)
    {
        var pathToZip1 = Path.Combine(Environment.CurrentDirectory, "Apps", app1);
        var pathToZip2 = Path.Combine(Environment.CurrentDirectory, "Apps", app2);

        // When there's a file content mismatch on non-JSON files,
        // we must throw an error and not use JSON to compare non JSON-files
        var exception = Assert.ThrowsExactly<ArgumentException>(() => MsAppTest.Compare(pathToZip1, pathToZip2, Console.Out));
        exception.Message.Should().Be("Mismatch detected in non-Json properties: Assets\\Images\\1556681b-11bd-4d72-9b17-4f884fb4b465.png");
    }
}
