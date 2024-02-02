// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.Yaml;

namespace Persistence.Tests.Yaml;

[TestClass]
public class DeserializerValidTests : TestBase
{
    [TestMethod]
    [DataRow("I am a screen with spaces", "42")]
    [DataRow("NoSpaces", "-50")]
    [DataRow("Yaml : | > ", "")]
    [DataRow("Text`~!@#$%^&*()_-+=", ":")]
    [DataRow("Text[]{};':\",.<>?/\\|", "@")]
    [DataRow("こんにちは", "#")]
    [DataRow("Cos'è questo?", "---")]
    public void Deserialize_ShouldParseSimpleStructure(string textValue, string xValue)
    {
        var graph = new Screen("Screen1")
        {
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = textValue } },
                { "X", new() { Value = xValue } },
                { "Y", new() { Value = "71" } },
            },
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.ControlUri.Should().Be(BuiltInTemplates.Screen);
        sut.Controls.Should().NotBeNull().And.BeEmpty();
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(3)
                .And.ContainKeys("Text", "X", "Y");
        sut.Properties["Text"].Value.Should().Be(textValue);
        sut.Properties["X"].Value.Should().Be(xValue);
        sut.Properties["Y"].Value.Should().Be("71");
    }

    [TestMethod]
    public void Deserialize_ShouldParseYamlWithChildNodes()
    {
        var graph = new Screen("Screen1")
        {
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a screen" }  },
            },
            Controls = new Control[]
            {
                new Text("Label1")
                {
                    Properties = new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "lorem ipsum" }  },
                    },
                },
                new Button("Button1")
                {
                    Properties = new Dictionary<string, ControlPropertyValue>()
                    {
                        { "Text", new() { Value = "click me" }  },
                        { "X", new() { Value = "100" } },
                        { "Y", new() { Value = "200" } }
                    },
                }
            }
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<Screen>();
        sut.Name.Should().Be("Screen1");
        sut.ControlUri.Should().Be(BuiltInTemplates.Screen);
        sut.Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Properties["Text"].Value.Should().Be("I am a screen");

        sut.Controls.Should().NotBeNull().And.HaveCount(2);
        sut.Controls![0].Should().BeOfType<Text>();
        sut.Controls![0].Name.Should().Be("Label1");
        sut.Controls![0].ControlUri.Should().Be(BuiltInTemplates.Text);
        sut.Controls![0].Properties.Should().NotBeNull()
                .And.HaveCount(1)
                .And.ContainKey("Text");
        sut.Controls![0].Properties["Text"].Value.Should().Be("lorem ipsum");

        sut.Controls![1].Should().BeOfType<Button>();
        sut.Controls![1].Name.Should().Be("Button1");
        sut.Controls![1].ControlUri.Should().Be(BuiltInTemplates.Button);
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
        var graph = new CustomControl("CustomControl1")
        {
            ControlUri = "http://localhost/#customcontrol",
            Properties = new Dictionary<string, ControlPropertyValue>()
            {
                { "Text", new() { Value = "I am a custom control" } },
            },
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

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
        ControlTemplateStore.TryGetControlTemplateByName("ButtonCanvas", out var buttonTemplate);

        var graph = new CustomControl("MyCustomButton")
        {
            ControlUri = buttonTemplate!.Uri,
        };

        var serializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateSerializer();
        var yaml = serializer.Serialize(graph);

        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();

        var sut = deserializer.Deserialize<Control>(yaml);
        sut.Should().NotBeNull().And.BeOfType<BuiltInControl>();
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-controls.fx.yaml", "Screen 1", 2, 2)]
    [DataRow(@"_TestData/ValidYaml/Screen-with-name.fx.yaml", "My Power Apps Screen", 0, 0)]
    [DataRow(@"_TestData/ValidYaml/Control-with-custom-template.yaml", "My Power Apps Custom Control", 0, 9)]
    public void Deserialize_ShouldSucceed(string path, string expectedName, int controlCount, int propertiesCount)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize(yamlReader);
        controlObj.Should().BeAssignableTo<Control>();
        var control = controlObj as Control;
        control!.Name.Should().NotBeNull().And.Be(expectedName);
        control.Controls.Should().NotBeNull().And.HaveCount(controlCount);
        control.Properties.Should().NotBeNull().And.HaveCount(propertiesCount);
    }

    [TestMethod]
    [DataRow(@"_TestData/ValidYaml/Screen-with-unmatched-field.fx.yaml")]
    public void Deserialize_ShouldIgnoreUnmatchedProperties(string path)
    {
        // Arrange
        var deserializer = ServiceProvider.GetRequiredService<IYamlSerializationFactory>().CreateDeserializer();
        using var yamlStream = File.OpenRead(path);
        using var yamlReader = new StreamReader(yamlStream);

        // Act
        var controlObj = deserializer.Deserialize(yamlReader);

        // Assert
        controlObj.Should().NotBeNull();
    }
}
