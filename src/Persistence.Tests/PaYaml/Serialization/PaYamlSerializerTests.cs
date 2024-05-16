// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3_0;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

namespace Persistence.Tests.PaYaml.Serialization;

[TestClass]
public class PaYamlSerializerTests : VSTestBase
{
    #region Deserialize Examples

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/App.pa.yaml", 5)]
    public void DeserializeExamplePaYamlApp(string path, int expectedAppPropertiesCount)
    {
        var paFileRoot = YamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
        paFileRoot.ShouldNotBeNull();

        // Top level properties
        paFileRoot.App.ShouldNotBeNull();
        paFileRoot.ComponentDefinitions.Should().BeNullOrEmpty();
        paFileRoot.Screens.Should().BeNullOrEmpty();

        paFileRoot.App.Properties.Should().NotBeNull()
            .And.HaveCount(expectedAppPropertiesCount);
        paFileRoot.App.Should().NotDefineMember("Children", "App.Children is still under design review");
    }

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/Screen1.pa.yaml", 2, 8, 14, 2, 3)]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/FormsScreen2.pa.yaml", 0, 1, 62, 0, 0)]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/ComponentsScreen4.pa.yaml", 0, 6, 6, 0, 0)]
    public void DeserializeExamplePaYamlScreen(string path, int expectedScreenPropertiesCount, int expectedScreenChildrenCount, int expectedDescendantsCount, int expectedScreenGroupsCount, int expectedTotalGroupsCount)
    {
        var paFileRoot = YamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
        paFileRoot.ShouldNotBeNull();

        // Top level properties
        paFileRoot.App.Should().BeNull();
        paFileRoot.ComponentDefinitions.Should().BeNullOrEmpty();
        paFileRoot.Screens.ShouldNotBeNull();

        // Check screen counts
        paFileRoot.Screens.Should().HaveCount(1);
        var screen = paFileRoot.Screens.First().Value;
        screen.Properties.Should().HaveCount(expectedScreenPropertiesCount);
        screen.Children.Should().HaveCount(expectedScreenChildrenCount);
        screen.DescendantControlInstances().Should().HaveCount(expectedDescendantsCount);

        screen.Groups.Should().HaveCount(expectedScreenGroupsCount);
        screen.DescendantControlInstances().SelectMany(nc => nc.Value.Groups).Should().HaveCount(expectedTotalGroupsCount - expectedScreenGroupsCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Components/MyHeaderComponent.pa.yaml", 9, 6, 1)]
    public void DeserializeExamplePaYamlComponentDefinition(string path, int expectedCustomPropertiesCount, int expectedPropertiesCount, int expectedChildrenCount)
    {
        var paFileRoot = YamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
        paFileRoot.ShouldNotBeNull();

        // Top level properties
        paFileRoot.App.Should().BeNull();
        paFileRoot.ComponentDefinitions.ShouldNotBeNull();
        paFileRoot.Screens.Should().BeNullOrEmpty();

        // Check components counts
        paFileRoot.ComponentDefinitions.Should().HaveCount(1);
        var componentDefinition = paFileRoot.ComponentDefinitions.First().Value;
        componentDefinition.Properties.Should().HaveCount(expectedPropertiesCount);
        componentDefinition.CustomProperties.Should().HaveCount(expectedCustomPropertiesCount);
        componentDefinition.Children.Should().HaveCount(expectedChildrenCount);
    }

    [TestMethod]
    public void DeserializeExamplePaYamlSingleFileApp()
    {
        var path = @"_TestData/SchemaV3_0/Examples/Single-File-App.pa.yaml";
        var paFileRoot = YamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
        paFileRoot.ShouldNotBeNull();

        // Top level properties
        paFileRoot.App.ShouldNotBeNull();
        paFileRoot.ComponentDefinitions.Should().BeNullOrEmpty();
        paFileRoot.Screens.ShouldNotBeNull();

        // Check random spot in the document
        paFileRoot.App.Properties.ShouldNotBeNull();
        paFileRoot.App.Properties.Should().ContainNames("BackEnabled", "Theme");
        var screen = paFileRoot.Screens.Should().ContainName("Screen1").WhoseValue;
        screen.Properties.Should().BeNullOrEmpty();
        screen.Children.Should().HaveCount(2)
            .And.ContainNames("Label1", "TextInput1");
    }

    #endregion

    #region RoundTrip from yaml

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/App.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/Screen1.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/FormsScreen2.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/ComponentsScreen4.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Components/MyHeaderComponent.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Single-File-App.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/Examples/AmbiguousComponentNames.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/FullSchemaUses/App.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/FullSchemaUses/Screens-general-controls.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/FullSchemaUses/Screens-with-components.pa.yaml")]
    public void RoundTripFromYaml(string path)
    {
        var originalYaml = File.ReadAllText(path);
        var paFileRoot = YamlSerializer.Deserialize<PaModule>(originalYaml);
        paFileRoot.ShouldNotBeNull();

        var roundTrippedYaml = YamlSerializer.Serialize(paFileRoot);
        TestContext.WriteTextWithLineNumbers(roundTrippedYaml, "roundTrippedYaml:");
        roundTrippedYaml.Should().BeYamlEquivalentTo(originalYaml);
    }

    #endregion
}
