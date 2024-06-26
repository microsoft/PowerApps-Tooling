// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Persistence.Tests.MsApp;

[TestClass]
public class MsappArchiveSaveTests : TestBase
{
    [TestMethod]
    [DataRow(@"  Hello   ", $"src/Hello.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello1.pa.yaml")]
    [DataRow(@"..\..\Hello", $"src/....Hello.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello2.pa.yaml")]
    [DataRow(@"c:\win\..\..\Hello", $"src/cWin....Hello.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello3.pa.yaml")]
    [DataRow(@"//..?HelloScreen", $"src/..HelloScreen.pa.yaml", @"_TestData/ValidYaml-CI/Screen-Hello4.pa.yaml")]
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
        msappValidation.DoesEntryExist(screenEntryName).Should().BeTrue();
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
        msappValidation.DoesEntryExist(screenEntryName).Should().BeTrue();
        using var streamReader = new StreamReader(msappValidation.GetRequiredEntry(screenEntryName).Open());
        var yaml = streamReader.ReadToEnd().NormalizeNewlines();
        var expectedYaml = File.ReadAllText(expectedYamlPath).NormalizeNewlines();
        yaml.Should().Be(expectedYaml);

        // Validate editor state
        if (msappValidation.DoesEntryExist(editorStateName))
        {
            using var editorStateReader = new StreamReader(msappValidation.GetRequiredEntry(editorStateName).Open());
            var json = editorStateReader.ReadToEnd().ReplaceLineEndings();
            var expectedJson = File.ReadAllText(expectedJsonPath).ReplaceLineEndings().TrimEnd();
            json.Should().Be(expectedJson);
        }
    }


    [TestMethod]
    [DataRow("HelloScreen")]
    public void Msapp_ShouldSave_App(string screenName)
    {
        // Arrange
        var tempFile = Path.Combine(TestContext.DeploymentDirectory!, Path.GetRandomFileName());
        using (var msappArchive = MsappArchiveFactory.Create(tempFile))
        {
            msappArchive.App.Should().BeNull();

            // Act
            var app = ControlFactory.CreateApp();
            app.Screens.Add(ControlFactory.CreateScreen(screenName));
            msappArchive.App = app;

            msappArchive.Save();
        }

        // Assert
        using var msappValidation = MsappArchiveFactory.Open(tempFile);
        msappValidation.App.Should().NotBeNull();
        msappValidation.App!.Screens.Count.Should().Be(1);
        msappValidation.App.Screens.Single().Name.Should().Be(screenName);
        msappValidation.App.Name.Should().Be(App.ControlName);
        msappValidation.DoesEntryExist(MsappArchive.HeaderFileName).Should().BeTrue();
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
        msappArchive.DoesEntryExist(Path.Combine(MsappArchive.Directories.Src, @$"SameName{sameNames.Length + 1}.pa.yaml")).Should().BeTrue();
    }
}
