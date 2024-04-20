// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializeControlsTests : TestBase
{
    [TestMethod]
    [DataRow(@"_TestData/ValidYaml-CI/BuiltInControl/button-classic.pa.yaml", true, "http://microsoft.com/appmagic/button")]
    //[DataRow(@"_TestData/ValidYaml-CI/BuiltInControl/button-modern.pa.yaml", false)]
    public void Deserialize_Classic_Should_Succeed(string path, bool isClassic, string expectedTemplateId)
    {
        // Arrange
        var deserializer = CreateDeserializer(true);
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var control = deserializer.Deserialize<Control>(yamlReader) as BuiltInControl;
        control.ShouldNotBeNull();
        control.Template.Should().NotBeNull();
        control.Template.IsClassic.Should().Be(isClassic);
        control.Template.Id.Should().Be(expectedTemplateId);
    }
}
