// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Tests.Yaml;

[TestClass]
public class YamlDeserializerTests
{
    [TestMethod]
    public void Deserialize_ShouldParseSimpleStructure()
    {
        var graph = new Screen()
        {
            Name = "Screen1",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" } },
                { "X", new() { Value = "42" } },
                { "Y", new() { Value = "71" } },
            },
        };

        var serializer = YamlSerializationFactory.CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = YamlSerializationFactory.CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.ControlUri.Should().Be(BuiltInTemplatesUris.Screen);
        sut.Controls.Should().NotBeNull().And.BeEmpty();
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(3)
                .And.ContainKeys("Text", "X", "Y");
        sut.Properties["Text"].Value.Should().Be("I am a screen");
        sut.Properties["X"].Value.Should().Be("42");
        sut.Properties["Y"].Value.Should().Be("71");
    }

    [TestMethod]
    public void Deserialize_ShouldParseYamlWithChildNodes()
    {
        var graph = new Screen()
        {
            Name = "Screen1",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" }  },
            },
            Controls = new Control[]
            {
                new Text()
                {
                    Name = "Label1",
                    Properties = new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "lorem ipsum" }  },
                    },
                },
                new Button()
                {
                    Name = "Button1",
                    Properties = new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "click me" }  },
                        { "X", new() { Value = "100" } },
                        { "Y", new() { Value = "200" } }
                    },
                }
            }
        };

        var serializer = YamlSerializationFactory.CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = YamlSerializationFactory.CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.ControlUri.Should().Be(BuiltInTemplatesUris.Screen);
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("I am a screen");

        sut.Controls.Should().NotBeNull().And.HaveCount(2);
        sut.Controls![0].Should().BeOfType<Text>();
        sut.Controls![0].Name.Should().Be("Label1");
        sut.Controls![0].ControlUri.Should().Be(BuiltInTemplatesUris.Text);
        sut.Controls![0].Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Controls![0].Properties["Text"].Value.Should().Be("lorem ipsum");

        sut.Controls![1].Should().BeOfType<Button>();
        sut.Controls![1].Name.Should().Be("Button1");
        sut.Controls![1].ControlUri.Should().Be(BuiltInTemplatesUris.Button);
        sut.Controls![1].Properties.Should().NotBeNull()
                .And.HaveCount(3)
                .And.ContainKeys("Text", "X", "Y");
        sut.Controls![1].Properties["Text"].Value.Should().Be("click me");
        sut.Controls![1].Properties["X"].Value.Should().Be("100");
        sut.Controls![1].Properties["Y"].Value.Should().Be("200");
    }

    [TestMethod]
    public void Deserialize_ShouldParseYamlForCustomControl()
    {
        var graph = new CustomControl()
        {
            ControlUri = "http://localhost/#customcontrol",
            Name = "CustomControl1",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a custom control" } },
            },
        };

        var serializer = YamlSerializationFactory.CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = YamlSerializationFactory.CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<CustomControl>();
        sut.Name.Should().Be("CustomControl1");
        sut.ControlUri.Should().Be("http://localhost/#customcontrol");
        sut.Controls.Should().NotBeNull().And.BeEmpty();
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("I am a custom control");
    }

    [TestMethod]
    public void Deserialize_ShouldParseBuiltInControlFromYamlCustomControl()
    {
        var graph = new CustomControl()
        {
            ControlUri = BuiltInTemplatesUris.Button,
            Name = "MyCustomButton",
        };

        var serializer = YamlSerializationFactory.CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = YamlSerializationFactory.CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Button>();
    }
}
