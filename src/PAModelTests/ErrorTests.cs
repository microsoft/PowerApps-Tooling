// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.Formulas.Tools;
using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace PAModelTests
{
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
            MemoryStream ms = new MemoryStream();

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

            string path1 = Path.Combine(Environment.CurrentDirectory, "TestData", file1);
            string path2 = Path.Combine(Environment.CurrentDirectory, "TestData", file2);
            string path3 = Path.Combine(Environment.CurrentDirectory, "TestData", file3);

            ErrorContainer errorContainer = new ErrorContainer();

            byte[] jsonString1 = File.ReadAllBytes(path1);
            byte[] jsonString2 = File.ReadAllBytes(path2);

            var jsonDictionary1 = MsAppTest.FlattenJson(jsonString1);
            var jsonDictionary2 = MsAppTest.FlattenJson(jsonString2);

            // IsMismatched on mismatched files
            MsAppTest.CheckPropertyChangedRemoved(jsonDictionary1, jsonDictionary2, errorContainer, "");
            MsAppTest.CheckPropertyAdded(jsonDictionary1, jsonDictionary2, errorContainer, "");

            // Confirm that the unit tests have the expected output
            Assert.Equal(File.ReadAllText(path3), errorContainer.ToString());
        }
    }
}
