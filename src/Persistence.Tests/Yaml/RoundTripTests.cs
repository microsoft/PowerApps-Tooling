// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using Persistence.Tests.Extensions;

namespace Persistence.Tests.Yaml;

[TestClass]
public class RoundTripTests
{
    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-name.fx.yaml", typeof(Screen), "My Power Apps Screen")]
    [DataRow(@"_TestData/ValidYaml/Screen-with-controls.fx.yaml", typeof(Screen), "Screen 1")]
    [DataRow(@"_TestData/ValidYaml/Control-with-custom-template.yaml", typeof(CustomControl), "My Power Apps Custom Control")]
    public void RoundTrip_ValidYaml(string path, Type rootType, string expectedName)
    {
        var deserializer = YamlSerializationFactory.CreateDeserializer();
        var serializer = YamlSerializationFactory.CreateSerializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Deserialize the yaml into an object.
        var controlObj = deserializer.Deserialize(yamlReader);
        controlObj.Should().BeAssignableTo(rootType);
        var control = (Control)controlObj!;
        control.Name.Should().Be(expectedName);

        // Serialize the object back into yaml.
        var actualYaml = serializer.Serialize(controlObj).NormalizeNewlines();

        // Assert that the yaml is the same.
        var expectedYaml = File.ReadAllText(path).NormalizeNewlines();
        actualYaml.Should().Be(expectedYaml);
    }
}
