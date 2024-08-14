// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializerValidAppTests : TestBase
{
    [TestMethod]
    //[DataRow(@"_TestData/ValidYaml{0}/App/with-screens.pa.yaml", true, typeof(App), "http://microsoft.com/appmagic/appinfo", "App", 2, 0, 1)]
    [DataRow(@"_TestData/ValidYaml{0}/App/with-screens.pa.yaml", true, typeof(App), "http://microsoft.com/appmagic/appinfo", "App", 2, 0, 1)]
    public void Deserialize_App_Should_Succeed(string path, bool isControlIdentifiers, Type expectedType, string expectedTemplateId,
        string expectedName, int screenCount, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var app = deserializer.Deserialize<App>(yamlReader);

        // Assert
        app.Should().BeAssignableTo(expectedType);
        app!.TemplateId.Should().NotBeNull().And.Be(expectedTemplateId);
        app!.Name.Should().NotBeNull().And.Be(expectedName);
        app!.Screens.Should().NotBeNull().And.HaveCount(screenCount);
        if (controlCount > 0)
            app.Children.Should().NotBeNull().And.HaveCount(controlCount);
        else
            app.Children.Should().BeNull();
        app.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }
}
