// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializerInvalidTests
{
    [TestMethod]
    public void Deserialize_ShouldFailWhenYamlIsInvalid()
    {
        // Arrange
        var deserializer = TestBase.ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var files = Directory.GetFiles(@"_TestData/InvalidYaml", $"*.fx.yaml", SearchOption.AllDirectories);
        // Uncomment to test single file
        // var files = new string[] { @"_TestData/InvalidYaml/Screen-with-host.fx.yaml" };

        Parallel.ForEach(files, file =>
        {
            using var yamlStream = File.OpenRead(file);
            using var yamlReader = new StreamReader(yamlStream);

            // Act
            try
            {
                var result = deserializer.Deserialize(yamlReader);
                if (result is not Control)
                    throw new InvalidOperationException("Expected a control");

                Assert.Fail($"Expected exception for file {file}");
            }
            catch (Exception ex) when (ex is not AssertFailedException)
            {
                // Assert exceptions are thrown
            }
        });
    }
}
