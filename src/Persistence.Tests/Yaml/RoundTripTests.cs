// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using Persistence.Tests.Extensions;

namespace Persistence.Tests.Yaml;

[TestClass]
public class RoundTripTests : TestBase
{
    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-name.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/Screen-with-controls.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-two-properties.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Hello", 2, 0)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-properties-and-controls.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen with two properties and two controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-properties-and-nested-controls.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen with two properties and two nested controls", 2, 2)]
    [DataRow(@"_TestData/ValidYaml/Screen/with-multiline-properties.fx.yaml", typeof(Screen), "http://microsoft.com/appmagic/screen", "Screen with two multiline properties", 2, 0)]
    [DataRow(@"_TestData/ValidYaml/Control-with-custom-template.yaml", typeof(CustomControl), "http://localhost/#customcontrol", "My Power Apps Custom Control", 9, 0)]
    [DataRow(@"_TestData/ValidYaml/BuiltInControl1.yaml", typeof(BuiltInControl), "http://microsoft.com/appmagic/powercontrol/PowerApps_CoreControls_ButtonCanvas", "BuiltIn Control1", 1, 0)]
    public void RoundTrip_ValidYaml(string path, Type rootType, string expectedTemplateId, string expectedName, int expectedPropsCount, int expectedControlCount)
    {
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Deserialize the yaml into an object.
        var controlObj = deserializer.Deserialize(yamlReader);

        // Validate the control.
        controlObj.Should().BeAssignableTo(rootType);
        var control = (Control)controlObj!;
        control.TemplateId.Should().Be(expectedTemplateId);
        control.Name.Should().Be(expectedName);
        control.Properties.Should().HaveCount(expectedPropsCount);
        control.Children.Should().HaveCount(expectedControlCount);

        // Serialize the object back into yaml.
        var actualYaml = serializer.Serialize(controlObj).NormalizeNewlines();

        // Assert that the yaml is the same.
        var expectedYaml = File.ReadAllText(path).NormalizeNewlines();
        actualYaml.Should().Be(expectedYaml);
    }
}
