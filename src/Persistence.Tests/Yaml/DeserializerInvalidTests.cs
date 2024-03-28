// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;
using YamlDotNet.Core;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializerInvalidTests
{
    [TestMethod]
    public void Deserialize_ShouldFailWhenYamlIsInvalid()
    {
        // Arrange
        var deserializer = TestBase.ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var files = Directory.GetFiles(@"_TestData/InvalidYaml", $"*.pa.yaml", SearchOption.AllDirectories);
        // Uncomment to test single file
        // var files = new string[] { @"_TestData/InvalidYaml/Screen-with-host.pa.yaml" };

        var failedFiles = 0;
        Parallel.ForEach(files, file =>
        {
            using var yamlStream = File.OpenRead(file);
            using var yamlReader = new StreamReader(yamlStream);

            // Act
            try
            {
                var result = deserializer.DeserializeControl<Control>(yamlReader);
                if (result is not Control)
                    throw new InvalidOperationException("Expected a control");

                Assert.Fail($"Expected exception for file {file}");
            }
            catch (Exception ex) when (ex is not AssertFailedException)
            {
                // Assert exceptions are thrown
                Interlocked.Increment(ref failedFiles);
            }
        });

        // Assert
        failedFiles.Should().BeGreaterThan(0);
        failedFiles.Should().Be(files.Length, "all files should fail");
    }

    [TestMethod]
    public void Deserialize_ShouldFailWhenExpectingDifferentType()
    {
        // Arrange
        var deserializer = TestBase.ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead("_TestData/ValidYaml/Screen/with-name.pa.yaml");
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        // Explicitly using the wrong type BuiltInControl
        Action act = () => { deserializer.DeserializeControl<BuiltInControl>(yamlReader); }; // Explicitly using the wrong type BuiltInControl

        // Assert
        act.Should().Throw<YamlException>()
            .WithInnerException<NotSupportedException>()
            .WithMessage("Cannot covert Microsoft.PowerPlatform.PowerApps.Persistence.Models.Screen to BuiltInControl");
    }

    [TestMethod]
    public void Deserialize_EmptyString()
    {
        // Arrange
        var deserializer = TestBase.ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        // Act
        Action act = () => deserializer.DeserializeControl<Control>(string.Empty);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'yaml')");
    }
}
