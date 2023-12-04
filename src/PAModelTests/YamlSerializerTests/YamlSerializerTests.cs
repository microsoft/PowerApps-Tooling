using FluentAssertions;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace PAModelTests.YamlSerializerTests;

#pragma warning disable SA1116

[TestClass]
public class YamlSerializerTests
{
    [TestMethod]
    [DataRow("John", "Doe")]
    [DataRow("Jörg/Jürgen", "Müller de Schäfer")]
    [DataRow("なまえ", "名前")]
    public void RoundtripSimpleObject(string firstName, string lastName)
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
        var serializer = new YamlPocoSerializer(new YamlWriter(writer));

        // Act
        serializer.Serialize(simpleObjectIn);

        // Assert
        var expectedYaml =
@$"Simple Object:
    First Name: {firstName}
    LastName: {lastName}
    Description: A simple object
    X: 20
    Age: 42
";
        writer.ToString().Should().Be(expectedYaml);

        // Arrange
        var deserializer = new YamlPocoDeserializer(new StringReader(expectedYaml))
        {
            Options = YamlLexerOptions.None
        };

        // Act
        var simpleObjectOut = deserializer.Deserialize<SimpleObject>();

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
    public void InvalidYaml(string invalidYaml, int errorLine)
    {
        // Arrange
        var deserializer = new YamlPocoDeserializer(new StringReader(invalidYaml))
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
        var deserializer = new YamlPocoDeserializer(new StringReader(invalidYaml))
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
