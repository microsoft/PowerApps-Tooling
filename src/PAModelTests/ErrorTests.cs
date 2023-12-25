// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using System.IO;
using Xunit;

namespace PAModelTests;

// Verify we get errors (and not exceptions). 
public class ErrorTests
{
    public static string PathToValidMsapp = Path.Combine(Environment.CurrentDirectory, "Apps", "MyWeather.msapp");
    public static string PathMissingMsapp = Path.Combine(Environment.CurrentDirectory, "Missing", "Missing.msapp");
    public static string PathMissingDir = Path.Combine(Environment.CurrentDirectory, "MissingDirectory");
    public static int counter = 0;

    [Fact]
    public void OpenCorruptedMsApp()
    {
        // Empty stream is invalid document, should generate a Read error.
        var ms = new MemoryStream();

        (var doc, var errors) = CanvasDocument.LoadFromMsapp(ms);
        Assert.True(errors.HasErrors);
        Assert.Null(doc);
    }

    [Fact]
    public void OpenMissingMsApp()
    {
        (var doc, var errors) = CanvasDocument.LoadFromMsapp(PathMissingMsapp);
        Assert.True(errors.HasErrors);
        Assert.Null(doc);
    }

    [Fact]
    public void OpenBadSources()
    {
        // Common error can be mixing up arguments. Ensure that we detect passing a valid msapp as a source param.
        Assert.True(File.Exists(PathToValidMsapp));

        (var doc, var errors) = CanvasDocument.LoadFromSources(PathToValidMsapp);
        Assert.True(errors.HasErrors);
        Assert.Null(doc);
    }

    [Fact]
    public void OpenMissingSources()
    {
        (var doc, var errors) = CanvasDocument.LoadFromSources(PathMissingDir);
        Assert.True(errors.HasErrors);
        Assert.Null(doc);
    }

    [Fact]
    public void BadWriteDir()
    {
        string path = null;
        Assert.Throws<DocumentException>(() => DirectoryWriter.EnsureFileDirExists(path));
    }

    [Theory]
    [InlineData("complexChanged1.json", "complexChanged2.json", "complexChangedOutput.txt")]
    [InlineData("complexAdded1.json", "complexAdded2.json", "complexAddedOutput.txt")]
    [InlineData("complexRemoved1.json", "complexRemoved2.json", "complexRemovedOutput.txt")]
    [InlineData("simpleChanged1.json", "simpleChanged2.json", "simpleChangedOutput.txt")]
    [InlineData("simpleAdded1.json", "simpleAdded2.json", "simpleAddedOutput.txt")]
    [InlineData("simpleRemoved1.json", "simpleRemoved2.json", "simpleRemovedOutput.txt")]
    [InlineData("emptyNestedArray1.json", "emptyNestedArray2.json", "emptyNestedArrayOutput.txt")]
    [InlineData("simpleArray1.json", "simpleArray2.json", "simpleArrayOutput.txt")]

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
        Assert.Equal(File.ReadAllText(path3), errorContainer.ToString());
    }

    [Theory]
    [InlineData("ImageApp_SwitchNames.msapp", "ImageApp.msapp")]
    [InlineData("ImageApp.msapp", "ImageApp_SwitchNames.msapp")]
    public void CompareChecksumImageNotReadAsJSONTest(string app1, string app2)
    {
        var pathToZip1 = Path.Combine(Environment.CurrentDirectory, "Apps", app1);
        var pathToZip2 = Path.Combine(Environment.CurrentDirectory, "Apps", app2);

        // When there's a file content mismatch on non-JSON files,
        // we must throw an error and not use JSON to compare non JSON-files
        var exception = Assert.Throws<ArgumentException>(() => MsAppTest.Compare(pathToZip1, pathToZip2, Console.Out));
        Assert.Equal("Mismatch detected in non-Json properties: Assets\\Images\\1556681b-11bd-4d72-9b17-4f884fb4b465.png", exception.Message);
    }
}
