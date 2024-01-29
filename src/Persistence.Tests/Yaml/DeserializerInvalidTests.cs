// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializerInvalidTests
{
    [TestMethod]
    public void Deserialize_ShouldFailWhenYamlIsInvalid()
    {
        // Arrange
        var deserializer = YamlSerializationFactory.CreateDeserializer();

        var files = Directory.GetFiles(@"_TestData/InvalidYaml", $"*{YamlUtils.YamlFxFileExtension}", SearchOption.AllDirectories);
        // Uncomment to test single file
        // var files = new string[] { @"_TestData/InvalidYaml/InvalidName.fx.yaml" };

        Parallel.ForEach(files, file =>
        {
            using var yamlStream = File.OpenRead(file);
            using var yamlReader = new StreamReader(yamlStream);

            // Act
            try
            {
                var screen = deserializer.Deserialize(yamlReader);
                Assert.Fail($"Expected exception for file {file}");
            }
            catch (Exception)
            {
                // Assert exceptions are thrown
            }
        });
    }
}
