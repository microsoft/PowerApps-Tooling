// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using YamlDotNet.Core;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializerInvalidTests : TestBase
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Deserialize_ShouldFailWhenYamlIsInvalid(bool isControlIdentifiers)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);

        var files = Directory.GetFiles(GetTestFilePath(@"_TestData/InvalidYaml{0}", isControlIdentifiers), $"*.pa.yaml", SearchOption.AllDirectories);
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
                var result = deserializer.Deserialize<Control>(yamlReader);
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
    [DataRow(false)]
    [DataRow(true)]
    public void Deserialize_ShouldFailWhenExpectingDifferentType(bool isControlIdentifiers)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath("_TestData/ValidYaml{0}/Screen/with-name.pa.yaml", isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        // Explicitly using the wrong type BuiltInControl
        Action act = () => { deserializer.Deserialize<BuiltInControl>(yamlReader); }; // Explicitly using the wrong type BuiltInControl

        // Assert
        act.Should().Throw<YamlException>()
            .WithInnerException<NotSupportedException>()
            .WithMessage("Cannot covert Microsoft.PowerPlatform.PowerApps.Persistence.Models.Screen to BuiltInControl");
    }

    [TestMethod]
    public void Deserialize_EmptyString()
    {
        // Arrange
        var deserializer = CreateDeserializer();

        // Act
        Action act = () => deserializer.Deserialize<Control>(string.Empty);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'yaml')");
    }

    public record TestSchema
    {
        public required Screen[] Screens { get; init; }
    }

    [TestMethod]
    [DataRow(@"_TestData/InvalidYaml{0}/screens-with-duplicates.pa.yaml", false)]
    [DataRow(@"_TestData/InvalidYaml{0}/screens-with-duplicates.pa.yaml", true)]
    public void Deserialize_Screens_List(string path, bool isControlIdentifiers)
    {
        // Arrange
        var deserializer = CreateDeserializer(isControlIdentifiers);
        using var yamlStream = File.OpenRead(GetTestFilePath(path, isControlIdentifiers));
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        Action act = () =>
        {
            // Explicitly using list of screens
            var result = deserializer.Deserialize<TestSchema>(yamlReader);
        };

        // Assert
        act.Should().Throw<YamlException>()
            .WithMessage("Duplicate control property*");
    }
}
