// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializeComponentDefinitionTests : TestBase
{
    [TestMethod]
    [DataRow(@"_TestData/ValidYaml{0}/ComponentDefinitions/with-custom-properties.pa.yaml", true, 6, 1, 3,
        "inputFuncImage", 1, "reqColorParam", "a required color param")]
    public void Deserialize_Component_Should_Succeed(string path, bool isControlIdentifiers,
        int customPropertiesCount, int childrenCount, int propertiesCount,
        string firstCustomProperyName, int firstCustomProperyParametersCount,
        string firstCustomProperyParameterName, string firstCustomProperyParameterDescription)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var component = deserializer.Deserialize<Control>(yamlReader) as ComponentDefinition
            ?? throw new InvalidOperationException("Failed to deserialize component");

        // Assert
        component.Should().NotBeNull();
        component.Children!.Count.Should().Be(childrenCount);
        component.Properties!.Count.Should().Be(propertiesCount);
        component.Template.Should().NotBeNull();
        component.Template!.Name.Should().Be("Component1");
        component.CustomProperties.Count.Should().Be(customPropertiesCount);
        component.CustomProperties[0].Name.Should().Be(firstCustomProperyName);
        component.CustomProperties[0].Parameters.Count.Should().Be(firstCustomProperyParametersCount);
        component.CustomProperties[0].Parameters[0].Name.Should().Be(firstCustomProperyParameterName);
        component.CustomProperties[0].Parameters[0].Description.Should().Be(firstCustomProperyParameterDescription);
    }

    [TestMethod]
    public void Deserialize_CommandComponentDefinition_Should_Succeed()
    {
        // Arrange
        var yaml = "Component1:\n  Control: Component\n  Type: Command";
        var deserializer = CreateDeserializer(true);
        using var reader = new StringReader(yaml);

        // Act
        var component = deserializer.Deserialize<Control>(reader) as ComponentDefinition;

        // Assert
        component.Should().NotBeNull();
        component.Name.Should().Be("Component1");
        component.Type.Should().Be(ComponentType.Command);
    }
}
