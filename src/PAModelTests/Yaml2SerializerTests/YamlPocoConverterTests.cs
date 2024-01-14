using FluentAssertions;
using Microsoft.PowerPlatform.Formulas.Tools;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml2;
using PAModelTests.Yaml2SerializerTests.YamlPocoTypes;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace PAModelTests.Yaml2SerializerTests;

[TestClass]
public class YamlPocoConverterTests
{
    [TestMethod]
    public void IntegersAndDoublesShouldSerializeCorrectly()
    {
        var obj = new NumberObject { Integer = 3, Double = Math.PI };
        var serializer = new YamlPocoConverter().Serializer;
        var serialized = serializer.Serialize(obj);

        var expected = $"integer: 3{Environment.NewLine}double: 3.141592653589793{Environment.NewLine}";
        serialized.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void OrderedTypeAppliesOnSerialization()
    {
        var obj = new OrderedObject
        {
            Arg3 = 5,
            Arg2 = "foo",
            Arg1 = true
        };

        var serializer = new YamlPocoConverter().Serializer;
        var serialized = serializer.Serialize(obj);

        var expected = @"arg1: true
arg2: foo
arg3: 5
";
        serialized.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void CanonicalizeShouldSortDisorderedProperties()
    {
        var initial = $"arg3: 3{Environment.NewLine}arg2: foo{Environment.NewLine}arg1: false{Environment.NewLine}";
        var actual = new YamlPocoConverter().Canonicalize<OrderedObject>(initial);
        var expected = $"arg1: false{Environment.NewLine}arg2: foo{Environment.NewLine}arg3: 3{Environment.NewLine}";

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void CanonicalizeShouldConvertMultilineStringToPreferredFormat()
    {
        var initial = @"value: >
  multiline

  string";
        var expected = @"value: |-
  multiline
  string
";
        var actual = new YamlPocoConverter().Canonicalize<StringObject>(initial);
        actual.Should().Be(expected);
    }

    [TestMethod]
    public void SerializerShouldQuoteSpecialStringValues()
    {
        // Most strings do not need quotes, but values which could be treated as literals of another type
        // such as numbers, "true", "false", "null", "no" etc can cause problems when not quoted
        var obj = new StringListObject { List = new List<string> { "no quotes needed", "1", "3.14", "false", "true", "null", "no" } };
        var expected = @"list:
- no quotes needed
- ""1""
- ""3.14""
- ""false""
- ""true""
- ""null""
- ""no""
";
        var actual = new YamlPocoConverter().Serializer.Serialize(obj);
        actual.Should().Be(expected);
    }

    [TestMethod]
    public void DeserializerShouldHandleSpecialValuesAsStringsEvenWithoutQuotes()
    {
        var withQuotes = @"list:
- no quotes needed
- ""1""
- ""3.14""
- ""false""
- ""true""
- ""no""
";
        var withoutQuotes = @"list:
- no quotes needed
- 1
- 3.14
- false
- true
- no
";
        var yamlConverter = new YamlPocoConverter();
        var actualFromQuoted = yamlConverter.Deserializer.Deserialize<StringListObject>(withQuotes);
        var actualFromNonQuoted = yamlConverter.Deserializer.Deserialize<StringListObject>(withoutQuotes);

        // Raw deserialization of non-quoted numbers or special values like true/false/no lead to
        // object values as numbers or bools, but the Deserializer should figure out the correct
        // type (string here) without the quotes, as we provided it with the expected type.
        // Note that null (sans quotes) cannot be read as a string, as null is a valid value. Those would
        // require quotes "null" to deserialize as a string value
        actualFromQuoted.List.Should().BeEquivalentTo(actualFromNonQuoted.List);
    }

    [TestMethod]
    public void DuplicateKeysDefinedOnTypeThrowsException()
    {
        var duplicateKeys = "collision: foo\ncollision: bar";
        var converter = new YamlPocoConverter();
        Action deserialize = () => converter.Deserializer.Deserialize<DuplicateKeyObject>(duplicateKeys);

        deserialize.Should().Throw<YamlException>().WithMessage("Multiple properties with the name/alias 'collision' already exists on type*");
    }

    [TestMethod]
    public void DuplicateKeysTargetingSamePropertyThrowsException()
    {
        var duplicateKeys = "value: foo\nvalue: bar";
        var converter = new YamlPocoConverter();
        Action deserialize = () => converter.Deserializer.Deserialize<StringObject>(duplicateKeys);

        deserialize.Should().Throw<YamlException>().WithMessage("Encountered duplicate key value");
    }

    [TestMethod]
    public void DefaultValuesSerializationTest()
    {
        var obj = new DefaultValuesObject
        {
            Foo = null, // DefaultValuesHandling.OmitDefaults, should not be serialized
            Bar = "bar", // DefaultValuesHandling.OmitDefaults, and DefaultValue = bar, should not be serialized
            Baz = null, // DefaultValuesHandling.Preserve, should serialize as empty
        };
        var expected = $"baz: {Environment.NewLine}";

        var actual = new YamlPocoConverter().Serializer.Serialize(obj);
        actual.Should().Be(expected);
    }

    [TestMethod]
    public void DefaultValuesDeserializationTest()
    {
        var yaml = $"baz: {Environment.NewLine}";
        var actual = new YamlPocoConverter().Deserializer.Deserialize<DefaultValuesObject>(yaml);
        actual.Foo.Should().BeNull();
        actual.Bar.Should().Be("bar");
        actual.Baz.Should().BeNull();
    }

    [TestMethod]
    public void YamlIgnorePreventsSerialization()
    {
        var obj = new IgnoreSomePropertiesObject
        {
            IncludeMe = "foo",
            IgnoreMe = "bar"
        };
        var expected = $"includeMe: foo{Environment.NewLine}";

        var actual = new YamlPocoConverter().Serializer.Serialize(obj);
        actual.Should().Be(expected);
    }

    [TestMethod]
    public void YamlIgnorePreventsDeserialization()
    {
        var yaml = @"includeMe: foo
ignoreMe: bar
";
        Action deserialize = () => new YamlPocoConverter().Deserializer.Deserialize<IgnoreSomePropertiesObject>(yaml);
        deserialize.Should().Throw<YamlException>().WithMessage("Property 'ignoreMe' not found on type *");
    }
}
