// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;

namespace PAModelTests.YamlSerializerTests;

#pragma warning disable SA1116

[TestClass]
public class YamlSerializerTests
{
    [TestMethod]
    [DataRow(null, "John", "Doe")]
    [DataRow("New Object Name", "Jörg/Jürgen", "Müller de Schäfer")]
    [DataRow("Simple Object", "なまえ", "名前")]
    public void RoundtripSimpleObject(string objectNameOverride, string firstName, string lastName)
    {
        // Arrange
        var simpleObjectIn = new SimpleObject
        {
            FirstName = firstName,
            LastName = lastName,
            Description = "A simple object",
            Age = 42,
            X = 20
        };

        var writer = new StringWriter();
        using var yamlWriter = new YamlWriter(writer);
        using var serializer = new YamlPocoSerializer(yamlWriter);

        // Act
        serializer.Serialize(simpleObjectIn, objectNameOverride);

        // Assert
        var expectedYaml =
@$"{(string.IsNullOrWhiteSpace(objectNameOverride) ? "Simple Object" : objectNameOverride)}:
    First Name: {firstName}
    LastName: {lastName}
    Description: A simple object
    X: 20
    Age: 42
";
        writer.ToString().Should().Be(expectedYaml);

        // Arrange
        using var deserializer = new YamlPocoDeserializer(new StringReader(expectedYaml))
        {
            Options = YamlLexerOptions.None
        };

        // Act
        var simpleObjectOut = deserializer.Deserialize<SimpleObject>(objectNameOverride);

        // Assert
        simpleObjectOut.Should().BeEquivalentTo(simpleObjectIn);
    }

    [TestMethod]
    [DataRow(@"", 0)]
    [DataRow(@"some garbage", 1)]
    [DataRow(@"some garbage:", 1)]
    [DataRow(@"Simple Object:some garbage", 1)]
    [DataRow(@"Simple Object: some garbage", 1)]
    [DataRow(@"Simple Object:
    X: abc", 2)]
    [DataRow(@"Simple Object:
    X:abc", 2)]
    public void InvalidYaml(string invalidYaml, int errorLine)
    {
        // Arrange
        using var deserializer = new YamlPocoDeserializer(new StringReader(invalidYaml))
        {
            Options = YamlLexerOptions.None
        };

        // Act
        Action action = () => deserializer.Deserialize<SimpleObject>();

        // Assert
        action.Should()
            .Throw<YamlParseException>()
            .Where(e => e.Line == errorLine);
    }

    [TestMethod]
    [DataRow(@"Invalid Object:
    X: abc", 0)]
    public void InvalidObjectWithDuplicateNames(string invalidYaml, int errorLine)
    {
        // Arrange
        using var deserializer = new YamlPocoDeserializer(new StringReader(invalidYaml))
        {
            Options = YamlLexerOptions.None
        };

        // Act
        Action action = () => deserializer.Deserialize<InvalidObjectWithDuplicateNames>();

        // Assert
        action.Should()
            .Throw<YamlParseException>()
            .Where(e => e.Line == errorLine);
    }
}
