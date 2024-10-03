// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Models.SchemaV3;
using Microsoft.PowerPlatform.PowerApps.Persistence.PaYaml.Serialization;

namespace Persistence.Tests.PaYaml.Serialization;

[TestClass]
public class PaYamlSerializerTests : VSTestBase
{
    [TestMethod]
    public void DeserializeNamedObjectSetsLocationInfo()
    {
        // Since App.Properties is a NamedObjectMapping, the location info should be set on the NamedObject
        var paModule = PaYamlSerializer.Deserialize<PaModule>("""
            App:
                Properties:
                    Foo: =true
                    Bar: ="hello world"
            """);
        paModule.ShouldNotBeNull();
        paModule.App.ShouldNotBeNull();
        paModule.App.Properties.ShouldNotBeNull();
        paModule.App.Properties.Should().ContainName("Foo").WhoseNamedObject.Start.Should().Be(new(3, 9));
        paModule.App.Properties.Should().ContainName("Bar").WhoseNamedObject.Start.Should().Be(new(4, 9));
    }

    #region Deserialize Examples

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/App.pa.yaml", 5)]
    public void DeserializeExamplePaYamlApp(string path, int expectedAppPropertiesCount)
    {
        var paFileRoot = PaYamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
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
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/DataSources/Dataversedatasources1.pa.yaml")]
    public void DeserializeExamplePaYamlDataSources(string path)
    {
        var paFileRoot = PaYamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
        paFileRoot.ShouldNotBeNull();

        // Top level properties
        paFileRoot.DataSources.ShouldNotBeNull();
        paFileRoot.App.Should().BeNull();
        paFileRoot.ComponentDefinitions.Should().BeNullOrEmpty();
        paFileRoot.Screens.Should().BeNullOrEmpty();
        paFileRoot.DataSources.Should().HaveCount(4);
        paFileRoot.DataSources.Should().AllSatisfy(ds => ds.Value.ConnectorId.Should().BeNull());
        paFileRoot.DataSources.GetValue("DataverseActions").Type.Should().Be(DataSourceType.Actions);

        // Assert all DataSources except DataverseActions are of type Table
        paFileRoot.DataSources
            .Where(ds => ds.Name != "DataverseActions")
            .Select(ds => ds.Value)
            .Should()
            .AllSatisfy(ds => ds.Type.Should().Be(DataSourceType.Table));
    }

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/Screen1.pa.yaml", 2, 8, 14, 2, 3)]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/FormsScreen2.pa.yaml", 0, 1, 62, 0, 0)]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/ComponentsScreen4.pa.yaml", 0, 6, 6, 0, 0)]
    public void DeserializeExamplePaYamlScreen(string path, int expectedScreenPropertiesCount, int expectedScreenChildrenCount, int expectedDescendantsCount, int expectedScreenGroupsCount, int expectedTotalGroupsCount)
    {
        var paFileRoot = PaYamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
        paFileRoot.ShouldNotBeNull();

        // Top level properties
        paFileRoot.App.Should().BeNull();
        paFileRoot.ComponentDefinitions.Should().BeNullOrEmpty();
        paFileRoot.Screens.ShouldNotBeNull();

        // Check screen counts
        paFileRoot.Screens.Should().HaveCount(1);
        var screen = paFileRoot.Screens.First().Value;
        if (expectedScreenPropertiesCount == 0)
            screen.Properties.Should().BeNull();
        else
            screen.Properties.Should().HaveCount(expectedScreenPropertiesCount);

        if (expectedScreenChildrenCount == 0)
            screen.Children.Should().BeNull();
        else
        {
            screen.Children.Should().HaveCount(expectedScreenChildrenCount);
            GetGroupCount(screen).Should().Be(expectedScreenGroupsCount);
        }

        if (expectedDescendantsCount == 0)
            screen.Properties.Should().BeNull();
        else
            screen.DescendantControlInstances().Should().HaveCount(expectedDescendantsCount);

        screen.DescendantControlInstances().Sum(nc => GetGroupCount(nc.Value)).Should().Be(expectedTotalGroupsCount - expectedScreenGroupsCount);

        static int GetGroupCount(IPaControlInstanceContainer container)
        {
            if (container.Children is null)
                return 0;

            return container.Children
                .Select(c => c.Value.GroupName)
                .Where(g => g != null)
                .Distinct()
                .Count();
        }
    }

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Components/MyHeaderComponent.pa.yaml", 9, 6, 1)]
    public void DeserializeExamplePaYamlComponentDefinition(string path, int expectedCustomPropertiesCount, int expectedPropertiesCount, int expectedChildrenCount)
    {
        var paFileRoot = PaYamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
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
        var paFileRoot = PaYamlSerializer.Deserialize<PaModule>(File.ReadAllText(path));
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

    [TestMethod]
    public void DeserializeDuplicateControlNamesShouldFail()
    {
        var path = @"_TestData/InvalidYaml-CI/duplicate-control-in-sequence.pa.yaml";
        var ex = Assert.ThrowsException<PersistenceLibraryException>(() => PaYamlSerializer.Deserialize<NamedObjectSequence<ControlInstance>>(File.ReadAllText(path)));
        ex.ErrorCode.Should().Be(PersistenceErrorCode.DuplicateNameInSequence);
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
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/DataSources/Dataversedatasources1.pa.yaml")]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Controls/control-with-namemap.pa.yaml")]
    public void RoundTripFromYaml(string path)
    {
        var originalYaml = File.ReadAllText(path);
        var paFileRoot = PaYamlSerializer.Deserialize<PaModule>(originalYaml);
        paFileRoot.ShouldNotBeNull();

        var roundTrippedYaml = PaYamlSerializer.Serialize(paFileRoot);
        TestContext.WriteTextWithLineNumbers(roundTrippedYaml, "roundTrippedYaml:");
        roundTrippedYaml.Should().BeYamlEquivalentTo(originalYaml);
    }

    #endregion

    #region Is Sequence checks

    [TestMethod]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/App.pa.yaml", false)]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Screens/Screen1.pa.yaml", false)]
    [DataRow(@"_TestData/SchemaV3_0/Examples/Src/Components/MyHeaderComponent.pa.yaml", false)]
    [DataRow(@"_TestData/ValidYaml-CI/With-list-of-controls.pa.yaml", true)]
    [DataRow(@"_TestData/InvalidYaml/Dupliacte-Keys.pa.yaml", false)]
    public void IsSequenceCheckShouldWorkAsExpected(string path, bool expected)
    {
        var yaml = File.ReadAllText(path);
        var isSequence = PaYamlSerializer.CheckIsSequence(yaml);
        isSequence.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not yaml")]
    [DataRow("{ prop1: str2 }")]
    public void IsSequenceCheckShouldReturnFalseForInvalidSequences(string yaml)
    {
        var isSequence = PaYamlSerializer.CheckIsSequence(yaml);
        isSequence.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("[]")]
    [DataRow("['str',333]")]
    public void IsSequenceCheckShouldReturnTrueForInlineSequences(string yaml)
    {
        var isSequence = PaYamlSerializer.CheckIsSequence(yaml);
        isSequence.Should().BeTrue();
    }

    [TestMethod]
    public void IsSequenceCheckShouldReturnTrueWhenSequenceInFragmentStartsWithComments()
    {
        var yaml = """
            # comment
            - name: control1
            - name: control2
            """;

        var isSequence = PaYamlSerializer.CheckIsSequence(yaml);
        isSequence.Should().BeTrue();
    }

    [TestMethod]
    public void IsSequenceCheckShouldReturnTrueWhenSequenceInFragmentBeginsWithDocumentStart()
    {
        var yaml = """
            ---
            - name: control1
            - name: control2
            """;

        var isSequence = PaYamlSerializer.CheckIsSequence(yaml);
        isSequence.Should().BeTrue();
    }

    #endregion
}
