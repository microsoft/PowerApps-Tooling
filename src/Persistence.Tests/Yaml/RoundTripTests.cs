// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Yaml;

[TestClass]
public class RoundTripTests : TestBase
{
    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-name.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-name.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-controls.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen-with-controls.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-two-properties.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Hello", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-two-properties.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Hello", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-controls.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-controls.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-nested-controls.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two nested controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-properties-and-nested-controls.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two properties and two nested controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-multiline-properties.pa.yaml", true, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two multiline properties", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Screen/with-multiline-properties.pa.yaml", false, typeof(Screen), "http://microsoft.com/appmagic/screen",
        "Screen with two multiline properties", 2, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Control-with-custom-template.pa.yaml", true, typeof(CustomControl), "http://localhost/#customcontrol",
        "My Power Apps Custom Control", 8, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/Control-with-custom-template.pa.yaml", false, typeof(CustomControl), "http://localhost/#customcontrol",
        "My Power Apps Custom Control", 8, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl1.pa.yaml", true, typeof(BuiltInControl), "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas",
        "BuiltIn Control1", 1, 0)]
    [DataRow(@"_TestData/ValidYaml{0}/BuiltInControl1.pa.yaml", false, typeof(BuiltInControl), "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas",
        "BuiltIn Control1", 1, 0)]
    public void RoundTrip_ValidYaml(string path, bool isControlIdentifiers, Type rootType, string expectedTemplateId, string expectedName, int expectedPropsCount, int expectedControlCount)
    {
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
}
