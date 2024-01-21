// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools.IO;

namespace PAModelTests;

[TestClass]
public class UtilityTests
{
    [DataTestMethod]
    [DataRow("\r\t!$^%/\\", "%0d%09%21%24%5e%25%2f%5c")]
    [DataRow("одиндваодиндваодиндваодиндваодиндваодинд", "одиндваодиндваодиндваодиндваодиндваодинд")]
    [DataRow("İkşzlerAçık芲偁ＡＢＣ巢für नमस्ते กุ้งจิ้яЧчŠš������  - Copy (2).jpg", "İkşzlerA%e7ık芲偁ＡＢＣ巢f%fcr नमस्ते กุ้งจิ้яЧчŠš������  - Copy %282%29.jpg")]
    public void TestEscaping(string unescaped, string escaped)
    {
        Assert.AreEqual(FilePath.EscapeFilename(unescaped), escaped);
        Assert.AreEqual(FilePath.UnEscapeFilename(escaped), unescaped);
    }

    [DataTestMethod]
    [DataRow("foo-%41", "foo-A")]
    [DataRow("[]_' ", "[]_' ")] // unescape only touches % character.
    [DataRow("İkşzlerA%e7ık芲偁ＡＢＣ巢f%fcr नमस्ते กุ้งจิ้яЧчŠš������  - Copy %282%29.jpg", "İkşzlerAçık芲偁ＡＢＣ巢für नमस्ते กุ้งจิ้яЧчŠš������  - Copy (2).jpg")]
    public void TestUnescape(string escaped, string unescaped)
    {
        Assert.AreEqual(FilePath.UnEscapeFilename(escaped), unescaped);
    }

    [TestMethod]
    public void TestNotEscaped()
    {
        // Not escaped.
        var a = "0123456789AZaz[]_. ";
        Assert.AreEqual(FilePath.EscapeFilename(a), a);
    }

    [DataTestMethod]
    [DataRow("C:\\Foo\\Bar\\file", "C:\\Foo", "Bar\\file")]
    [DataRow("C:\\Foo\\Bar\\file", "C:\\Foo\\", "Bar\\file")]
    [DataRow("C:\\Foo\\Bar.msapp", "C:\\Foo", "Bar.msapp")]
    [DataRow("C:\\Foo\\Bar.msapp", "C:\\Foo\\", "Bar.msapp")]
    [DataRow("C:\\Foo\\Bar.msapp", "C:\\", "Foo\\Bar.msapp")]
    [DataRow(@"C:\DataSources\JourneyPlanner|Sendforapproval.json", "C:\\", @"DataSources\JourneyPlanner|Sendforapproval.json")]
    [DataRow(@"C:\DataSources\JourneyPlanner%7cSendforapproval.json", "C:\\", @"DataSources\JourneyPlanner%7cSendforapproval.json")]
    [DataRow(@"d:\app\Src\EditorState\Screen%252.editorstate.json", @"d:\app", @"Src\EditorState\Screen%252.editorstate.json")]
    [DataRow(@"C:\Temp\MySolution\MySolution.Project\DataSources\JourneyPlanner%7cSendforapproval.json", @"C:\Temp\MySolution\MySolution.Project", @"DataSources\JourneyPlanner%7cSendforapproval.json")]
    [DataRow(@"D:\Testing\Power-fx\LocalTest\Unpack\Multi-Lang-App-MX\Assets\Images\İkşzlerA%e7ık芲偁ＡＢＣ巢f%fcr नमस्ते กุ้งจิ้яЧчŠš������  - Copy %282%29.jpg", @"D:\Testing\Power-fx\LocalTest\Unpack\Multi-Lang-App-MX\Assets", @"Images\İkşzlerA%e7ık芲偁ＡＢＣ巢f%fcr नमस्ते กุ้งจิ้яЧчŠš������  - Copy %282%29.jpg")]
    public void TestRelativePath(string fullPath, string basePath, string expectedRelativePath)
    {
        // Test non-windows paths if on other platforms
        if (Path.DirectorySeparatorChar != '\\')
        {
            fullPath = fullPath.Replace('\\', '/');
            basePath = basePath.Replace('\\', '/');
            expectedRelativePath = expectedRelativePath.Replace('\\', '/');
        }
        Assert.AreEqual(expectedRelativePath, FilePath.GetRelativePath(basePath, fullPath));
    }


    // Verify regression from
    // https://github.com/microsoft/PowerApps-Language-Tooling/issues/153
    [TestMethod]
    public void Regression153()
    {
        // Not escaped.
        var path = @"DataSources\JourneyPlanner|Sendforapproval.json";
        var escape = FilePath.EscapeFilename(path);

        var original = FilePath.UnEscapeFilename(escape);

        Assert.AreEqual(path, original);
    }

    [DataTestMethod]
    [DataRow("Long*Control*Name*Truncation*Tests***", "Long%2aControl%2aName%2aTruncation%2aTests%2a_959")]
    [DataRow("TestReallyLoooooooooooooooooooooooooooooooooooongControlName", "TestReallyLooooooooooooooooooooooooooooooooooo_cad")]
    [DataRow("TestControlName", "TestControlName")]
    public void TestControlNameTruncation(string originalName, string expectedName)
    {
        var truncatedName = FilePath.TruncateNameIfTooLong(originalName);
        Assert.AreEqual(truncatedName, expectedName);
    }
}
