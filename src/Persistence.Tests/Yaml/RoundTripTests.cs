// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Yaml;

[TestClass]
public class RoundTripTests : TestBase
{
    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-name.pa.yaml", true, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-name.pa.yaml", false, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-controls.pa.yaml", true, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-controls.pa.yaml", false, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-two-properties.pa.yaml", true, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Hello", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-two-properties.pa.yaml", false, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Hello", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-controls.pa.yaml", true, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-controls.pa.yaml", false, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-nested-controls.pa.yaml", true, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two nested controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-nested-controls.pa.yaml", false, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two nested controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-multiline-properties.pa.yaml", true, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two multiline properties", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-multiline-properties.pa.yaml", false, nameof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two multiline properties", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Control-with-custom-template.pa.yaml", true, nameof(CustomControl), "http://localhost/#customcontrol",
        "My Power Apps Custom Control", 8, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Control-with-custom-template.pa.yaml", false, nameof(CustomControl), "http://localhost/#customcontrol",
        "My Power Apps Custom Control", 8, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl1.pa.yaml", true, nameof(BuiltInControl), "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas",
        "BuiltIn Control1", 1, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl1.pa.yaml", false, nameof(BuiltInControl), "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas",
        "BuiltIn Control1", 1, 0)]
    public void RoundTrip_ValidYaml(string path, bool isControlIdentifiers, string rootTypeName, string expectedTemplateId, string expectedName, int expectedPropsCount, int expectedControlCount)
    {
        // We don't use Type for the test parameter so that the TestExplorer shows test cases individually
        var rootType = GetRootType(rootTypeName);

        var deserializer = CreateDeserializer(isControlIdentifiers);
        var serializer = CreateSerializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Deserialize the yaml into an object.
        var controlObj = deserializer.DeserializeControl(yamlReader, rootType);

        // Validate the control.
        controlObj.Should().BeAssignableTo(rootType);
        var control = (Control)controlObj!;
        control.TemplateId.Should().Be(expectedTemplateId);
        control.Name.Should().Be(expectedName);
        control.Properties.Should().HaveCount(expectedPropsCount);
        if (expectedControlCount > 0)
            control.Children.Should().HaveCount(expectedControlCount);
        else
            control.Children.Should().BeNull();

        // Serialize the object back into yaml.
        var actualYaml = serializer.SerializeControl(control).NormalizeNewlines();

        // Assert that the yaml is the same.
        var expectedYaml = File.ReadAllText(GetTestFilePath(path, isControlIdentifiers)).NormalizeNewlines();
        actualYaml.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-component-instance.pa.yaml", true, "MyComponentLibrary")]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-component-instance.pa.yaml", false, "MyComponentLibrary")]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-component-instance-no-library.pa.yaml", true, "")]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-component-instance-no-library.pa.yaml", false, "")]
    public void Screen_With_Component_Instance(string path, bool isControlIdentifiers, string componentLibraryName)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act I: Deserialize the yaml into an object.
        var screen = deserializer.Deserialize<Control>(yamlReader) as Screen
            ?? throw new InvalidOperationException("Failed to deserialize screen");

        // Assert
        screen.Children!.Count.Should().Be(1);
        var customControl = screen.Children[0] as ComponentInstance
            ?? throw new InvalidOperationException("Failed to deserialize component instance");
        customControl.Should().NotBeNull();
        customControl.Name.Should().Be("This is custom component");
        customControl.ComponentName.Should().Be("ComponentDefinition_1");
        customControl.ComponentLibraryUniqueName.Should().Be(componentLibraryName);

        // Act II: Serialize the object back into yaml.
        var serializer = CreateSerializer(isControlIdentifiers);
        var actualYaml = serializer.SerializeControl(screen).NormalizeNewlines();

        // Assert that the yaml is the same.
        var expectedYaml = File.ReadAllText(GetTestFilePath(path, isControlIdentifiers)).NormalizeNewlines();
        actualYaml.Should().Be(expectedYaml);
    }

    [TestMethod]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/App.pa.yaml", nameof(App))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/ComplexDataScreen.pa.yaml", nameof(Screen))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/ComponentsScreen4.pa.yaml", nameof(Screen))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/DataTableScreen3.pa.yaml", nameof(Screen))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/DuplicateOf_Screen1.pa.yaml", nameof(Screen))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/FormsScreen2.pa.yaml", nameof(Screen))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Screen1.pa.yaml", nameof(Screen))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Screen6.pa.yaml", nameof(Screen))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Components/AllCustomPropertyTypes.pa.yaml", nameof(ComponentDefinition))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Components/CommonHeader.pa.yaml", nameof(ComponentDefinition))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Components/label.pa.yaml", nameof(ComponentDefinition))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Components/MenuTemplate.pa.yaml", nameof(ComponentDefinition))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Components/MenuTemplate_2.pa.yaml", nameof(ComponentDefinition))]
    [DataRow(@"_TestData/CurrentYamlApps/joem-control-serialization/joem-control-serialization-from-test/Src/Components/MyHeaderComponent.pa.yaml", nameof(ComponentDefinition))]
    public void RoundTripExampleApp(string path, string rootTypeName)
    {
        // We don't use Type for the test parameter so that the TestExplorer shows test cases individually
        var rootType = GetRootType(rootTypeName);

        var deserializer = CreateDeserializer(isControlIdentifiers: true);
        var serializer = CreateSerializer(isControlIdentifiers: true);

        // Deserialize the yaml into an object.
        Control? deserializedObj;
        var originalYaml = File.ReadAllText(path);
        using (var yamlReader = new StringReader(originalYaml))
        {
            deserializedObj = (Control?)deserializer.DeserializeControl(yamlReader, rootType);
        }

        // Validate the control.
        deserializedObj.ShouldNotBeNull();
        deserializedObj.Should().BeAssignableTo(rootType);

        // Serialize the object back into yaml.
        var roundTrippedYaml = serializer.SerializeControl(deserializedObj);
        TestContext.WriteTextWithLineNumbers(roundTrippedYaml, "roundTrippedYaml:");
        roundTrippedYaml.Should().BeYamlEquivalentTo(originalYaml);
    }

    private static Type GetRootType(string rootTypeName)
    {
        return rootTypeName switch
        {
            nameof(App) => typeof(App),
            nameof(Screen) => typeof(Screen),
            nameof(ComponentDefinition) => typeof(ComponentDefinition),
            nameof(CustomControl) => typeof(CustomControl),
            nameof(BuiltInControl) => typeof(BuiltInControl),
            _ => throw new ArgumentException("Invalid root file type.", nameof(rootTypeName)),
        };
    }
}
