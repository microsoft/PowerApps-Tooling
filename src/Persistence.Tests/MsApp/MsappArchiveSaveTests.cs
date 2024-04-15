// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Compression;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Persistence.Tests.MsApp;

[TestClass]
public class MsappArchiveSaveTests : TestBase
{
    [TestMethod]
    [DataRow(@"  Hello   ", $"src/Hello.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello1.pa.yaml")]
    [DataRow(@"..\..\Hello", $"src/Hello.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello2.pa.yaml")]
    [DataRow(@"c:\win\..\..\Hello", $"src/cWinHello.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello3.pa.yaml")]
    [DataRow(@"//..?HelloScreen", $"src/HelloScreen.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello4.pa.yaml")]
    [DataRow(@"Hello Space", $"src/Hello Space.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello5.pa.yaml")]
    public void Msapp_ShouldSave_Screen(string screenName, string screenEntryName, string expectedYamlPath)
    {
        // Arrange
        var tempFile = Path.Combine(TestContext.DeploymentDirectory!, Path.GetRandomFileName());
        using var msappArchive = MsappArchiveFactory.Create(tempFile);

        msappArchive.App.Should().BeNull();

        // Act
        var screen = ControlFactory.CreateScreen(screenName);
        msappArchive.Save(screen);
        msappArchive.Dispose();

        // Assert
        using var msappValidation = MsappArchiveFactory.Open(tempFile);
        msappValidation.App.Should().BeNull();
        msappValidation.CanonicalEntries.Count.Should().Be(2);
        var screenEntry = msappValidation.CanonicalEntries[MsappArchive.NormalizePath(screenEntryName)];
        screenEntry.Should().NotBeNull();
        using var streamReader = new StreamReader(msappValidation.GetRequiredEntry(screenEntryName).Open());
        var yaml = streamReader.ReadToEnd().NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedYamlPath).NormalizeNewlines();
        yaml.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"  Hello   ", "My control",
        $"src/Hello.pa.yaml",
        $"{MsappArchive.Directories.Controls}/Hello.json",
        @"_TestData/ValidYaml-CI/Screen-with-control1.pa.yaml",
        @"_TestData/ValidYaml-CI/Screen-with-control1.json")]
    public void Msapp_ShouldSave_Screen_With_Control(string screenName, string controlName, string screenEntryName, string editorStateName,
        string expectedYamlPath, string expectedJsonPath)
    {
        // Arrange
        var tempFile = Path.Combine(TestContext.DeploymentDirectory!, Path.GetRandomFileName());
        using var msappArchive = MsappArchiveFactory.Create(tempFile);

        msappArchive.App.Should().BeNull();

        // Act
        var screen = ControlFactory.CreateScreen(screenName,
            children: new[] {
                ControlFactory.Create(controlName, "ButtonCanvas")
            });
        msappArchive.Save(screen);
        msappArchive.Dispose();

        // Assert
        using var msappValidation = MsappArchiveFactory.Open(tempFile);
        msappValidation.App.Should().BeNull();
        msappValidation.CanonicalEntries.Count.Should().Be(2);

        // Validate screen
        var screenEntry = msappValidation.CanonicalEntries[MsappArchive.NormalizePath(screenEntryName)];
        screenEntry.Should().NotBeNull();
        using var streamReader = new StreamReader(msappValidation.GetRequiredEntry(screenEntryName).Open());
        var yaml = streamReader.ReadToEnd().NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedYamlPath).NormalizeNewlines();
        yaml.Should().Be(expectedYaml);

        // Validate editor state
        if (msappValidation.CanonicalEntries.TryGetValue(MsappArchive.NormalizePath(editorStateName), out var editorStateEntry))
        {
            editorStateEntry.Should().NotBeNull();
            using var editorStateReader = new StreamReader(msappValidation.GetRequiredEntry(editorStateName).Open());
            var json = editorStateReader.ReadToEnd().ReplaceLineEndings();
            var expectedJson = File.ReadAllText(expectedJsonPath).ReplaceLineEndings().TrimEnd();
            json.Should().Be(expectedJson);
        }
    }


    [TestMethod]
    [DataRow("HelloWorld", "HelloScreen")]
    public void Msapp_ShouldSave_App(string appName, string screenName)
    {
        // Arrange
        var tempFile = Path.Combine(TestContext.DeploymentDirectory!, Path.GetRandomFileName());
        using (var msappArchive = MsappArchiveFactory.Create(tempFile))
        {
            msappArchive.App.Should().BeNull();

            // Act
            var app = ControlFactory.CreateApp(appName);
            app.Screens.Add(ControlFactory.CreateScreen(screenName));
            msappArchive.App = app;

            msappArchive.Save();
        }

        // Assert
        using var msappValidation = MsappArchiveFactory.Open(tempFile);
        msappValidation.App.Should().NotBeNull();
        msappValidation.App!.Screens.Count.Should().Be(1);
        msappValidation.App.Screens.Single().Name.Should().Be(screenName);
        msappValidation.App.Name.Should().Be(appName);
        msappValidation.CanonicalEntries.Keys.Should().Contain(MsappArchive.NormalizePath(MsappArchive.HeaderFileName));
    }

    [TestMethod]
    public void Msapp_ShouldSave_WithUniqueName()
    {
        // Arrange
        var tempFile = Path.Combine(TestContext.DeploymentDirectory!, Path.GetRandomFileName());
        using var msappArchive = MsappArchiveFactory.Create(tempFile);

        var sameNames = new string[] { "SameName", "Same..Name", @"..\SameName", "!SameName!", ".SameName", "SameName", "SAMENAME", "SameNAME", "SameName1", "{SameName}" };

        // Act
        for (var idx = 0; idx < sameNames.Length; idx++)
        {
            var screen = ControlFactory.CreateScreen(sameNames[idx]);
            msappArchive.Save(screen);

            // Assert
            msappArchive.CanonicalEntries.Count.Should().Be(idx + 1);
        }

        msappArchive.CanonicalEntries.Count.Should().Be(sameNames.Length);
        msappArchive.CanonicalEntries.Keys.Should().Contain(MsappArchive.NormalizePath(Path.Combine(MsappArchive.Directories.Src, @$"SameName{sameNames.Length + 1}{MsappArchive.YamlPaFileExtension}")));
    }

    [TestMethod]
    [DataRow(@"_TestData/AppsWithYaml/HelloWorld.msapp", "App", "HelloScreen")]
    public void Msapp_ShouldSaveAs_NewFilePath(string testDirectory, string appName, string screenName)
    {
        // Arrange
        var tempFile = Path.Combine(TestContext.DeploymentDirectory!, Path.GetRandomFileName());

        // Zip archive in memory from folder
        using var stream = new MemoryStream();
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var files = Directory.GetFiles(testDirectory, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                zipArchive.CreateEntryFromFile(file, file.Substring(testDirectory.Length + 1));
            }
        }
        using var testApp = MsappArchiveFactory.Update(stream, overwriteOnSave: true);

        // Save the test app to another file
        testApp.SaveAs(tempFile);

        // Open the app from the file
        using var msappValidation = MsappArchiveFactory.Open(tempFile);

        // Assert
        msappValidation.App.Should().NotBeNull();
        msappValidation.App!.Screens.Count.Should().Be(1);
        msappValidation.App.Screens.Single().Name.Should().Be(screenName);
        msappValidation.App.Name.Should().Be(appName);
        msappValidation.CanonicalEntries.Keys.Should().Contain(MsappArchive.NormalizePath(MsappArchive.HeaderFileName));
    }
}
